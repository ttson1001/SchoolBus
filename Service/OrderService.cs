using BE_API.Configuration;
using BE_API.Dto.Order;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using BE_API.Dto.Common;

namespace BE_API.Service
{
    public class OrderService : IOrderService
    {
        private const string WalletMethod = "WALLET";
        private const string PayOsOrderMethod = "PAYOS_ORDER";
        private const string PayOsPaymentMethod = "PAYOS";
        private const string SystemReceiver = "SYSTEM";

        private readonly IRepository<Order> _orderRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<BusRoute> _busRouteRepo;
        private readonly IRepository<Package> _packageRepo;
        private readonly IRepository<Wallet> _walletRepo;
        private readonly IRepository<Payment> _paymentRepo;
        private readonly IRepository<TransactionLog> _transactionLogRepo;
        private readonly PayOSClient _payOsClient;
        private readonly PayOsSettings _payOsSettings;

        public OrderService(
            IRepository<Order> orderRepo,
            IRepository<User> userRepo,
            IRepository<Student> studentRepo,
            IRepository<BusRoute> busRouteRepo,
            IRepository<Package> packageRepo,
            IRepository<Wallet> walletRepo,
            IRepository<Payment> paymentRepo,
            IRepository<TransactionLog> transactionLogRepo,
            PayOSClient payOsClient,
            IOptions<PayOsSettings> payOsOptions)
        {
            _orderRepo = orderRepo;
            _userRepo = userRepo;
            _studentRepo = studentRepo;
            _busRouteRepo = busRouteRepo;
            _packageRepo = packageRepo;
            _walletRepo = walletRepo;
            _paymentRepo = paymentRepo;
            _transactionLogRepo = transactionLogRepo;
            _payOsClient = payOsClient;
            _payOsSettings = payOsOptions.Value;
        }

        public async Task<OrderDto> CreateOrderAsync(OrderCreateDto dto)
        {
            var guardian = await ValidateGuardianAsync(dto.GuardianId);
            var student = await ValidateStudentAsync(dto.StudentId, dto.GuardianId);
            var package = await ValidatePackageAsync(dto.PackageId);
            var selectedRouteIds = await ValidateSelectedRoutesAsync(dto.RouteIds, student.CampusId, package.RouteLimit);

            await ExpireOrdersAsync(dto.StudentId);
            await EnsureStudentHasNoActiveOrderAsync(dto.StudentId);

            var wallet = await _walletRepo.Get()
                .FirstOrDefaultAsync(x => x.UserId == guardian.Id);

            if (wallet == null)
                throw new Exception("Guardian chưa có ví. Vui lòng nạp tiền trước");

            if (wallet.Balance < package.Price)
                throw new Exception("Số dư không đủ để mua gói");

            var now = DateTime.UtcNow;
            var oldBalance = wallet.Balance;
            var endDate = now.AddDays(package.DurationDays);

            wallet.Balance -= package.Price;
            _walletRepo.Update(wallet);

            var order = new Order
            {
                GuardianId = guardian.Id,
                StudentId = student.Id,
                BusRouteId = selectedRouteIds.Count == 1 ? selectedRouteIds[0] : null,
                SelectedRouteIds = JoinSelectedRouteIds(selectedRouteIds),
                PackageId = package.Id,
                Status = OrderStatus.PAID,
                StartDate = now,
                EndDate = endDate,
                PaidAt = now,
                CreatedAt = now
            };

            await _orderRepo.AddAsync(order);

            var payment = new Payment
            {
                Order = order,
                Method = WalletMethod,
                Amount = package.Price,
                Status = PaymentStatus.SUCCESS,
                PaidAt = now
            };

            await _paymentRepo.AddAsync(payment);

            var transactionLog = new TransactionLog
            {
                Order = order,
                Method = WalletMethod,
                Amount = package.Price,
                Status = PaymentStatus.SUCCESS.ToString(),
                PaidAt = now,
                OldBalance = oldBalance,
                NewBalance = wallet.Balance,
                Sender = guardian.Email,
                Receiver = SystemReceiver,
                Description = $"Thanh toan goi {package.Name} bang vi",
                Code = $"WALLET-{Guid.NewGuid():N}".ToUpperInvariant()
            };

            await _transactionLogRepo.AddAsync(transactionLog);
            await _orderRepo.SaveChangesAsync();

            var createdOrder = await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == order.Id)
                ?? throw new Exception("Order không tồn tại");

            return MapToDto(createdOrder);
        }

        public async Task<OrderPayOsLinkDto> CreatePayOsOrderLinkAsync(OrderPayOsCreateDto dto)
        {
            var guardian = await ValidateGuardianAsync(dto.GuardianId);
            var student = await ValidateStudentAsync(dto.StudentId, dto.GuardianId);
            var package = await ValidatePackageAsync(dto.PackageId);
            var selectedRouteIds = await ValidateSelectedRoutesAsync(dto.RouteIds, student.CampusId, package.RouteLimit);
            EnsurePayOsAmountSupported(package.Price);

            await ExpireOrdersAsync(dto.StudentId);
            await EnsureStudentHasNoActiveOrderAsync(dto.StudentId);

            var returnUrl = ResolveUrl(dto.ReturnUrl, _payOsSettings.ReturnUrl, "ReturnUrl");
            var cancelUrl = ResolveUrl(dto.CancelUrl, _payOsSettings.CancelUrl, "CancelUrl");
            var orderCode = await GeneratePayOsOrderCodeAsync();
            var description = BuildDirectOrderDescription(student.Id);
            var createdAt = DateTime.UtcNow;

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = decimal.ToInt64(package.Price),
                Description = description,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            };

            var paymentLink = await _payOsClient.PaymentRequests.CreateAsync(paymentRequest);

            var order = new Order
            {
                GuardianId = guardian.Id,
                StudentId = student.Id,
                BusRouteId = selectedRouteIds.Count == 1 ? selectedRouteIds[0] : null,
                SelectedRouteIds = JoinSelectedRouteIds(selectedRouteIds),
                PackageId = package.Id,
                Status = OrderStatus.PENDING,
                CreatedAt = createdAt
            };

            await _orderRepo.AddAsync(order);

            var transactionLog = new TransactionLog
            {
                Order = order,
                Method = PayOsOrderMethod,
                Amount = package.Price,
                Status = OrderStatus.PENDING.ToString(),
                OldBalance = 0,
                NewBalance = 0,
                Sender = guardian.Email,
                Receiver = SystemReceiver,
                Description = description,
                Code = orderCode.ToString()
            };

            await _transactionLogRepo.AddAsync(transactionLog);
            await _orderRepo.SaveChangesAsync();

            return new OrderPayOsLinkDto
            {
                OrderId = order.Id,
                GuardianId = guardian.Id,
                StudentId = student.Id,
                PackageId = package.Id,
                PackageName = package.Name,
                SelectedRouteIds = selectedRouteIds,
                PackageRouteLimit = package.RouteLimit,
                OrderCode = orderCode,
                Amount = package.Price,
                Description = description,
                CheckoutUrl = paymentLink.CheckoutUrl,
                Status = OrderStatus.PENDING.ToString(),
                CreatedAt = createdAt
            };
        }

        public async Task<OrderPayOsStatusDto> HandlePayOsWebhookAsync(Webhook webhook)
        {
            var verifiedData = await _payOsClient.Webhooks.VerifyAsync(webhook);

            if (IsPayOsUrlVerificationPing(verifiedData))
            {
                return new OrderPayOsStatusDto
                {
                    OrderId = 0,
                    GuardianId = 0,
                    StudentId = 0,
                    PackageId = 0,
                    PackageName = string.Empty,
                    OrderCode = verifiedData.OrderCode,
                    Amount = verifiedData.Amount,
                    OrderStatus = "PAYOS_URL_VERIFICATION",
                    TransactionStatus = "PAYOS_URL_VERIFICATION",
                    PaidAt = null,
                    StartDate = null,
                    EndDate = null,
                    CreatedAt = DateTime.UtcNow
                };
            }

            var transactionLog = await GetPayOsOrderTransactionAsync(verifiedData.OrderCode);
            var order = await GetOrderForTransactionAsync(transactionLog);

            if (order.Status == OrderStatus.PAID &&
                string.Equals(transactionLog.Status, PaymentStatus.SUCCESS.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return MapToPayOsStatusDto(order, transactionLog);
            }

            if (order.Status == OrderStatus.CANCELLED &&
                !string.Equals(transactionLog.Status, PaymentStatus.SUCCESS.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return MapToPayOsStatusDto(order, transactionLog);
            }

            var paidAt = ParseTransactionDateTime(verifiedData.TransactionDateTime);

            if (!string.Equals(verifiedData.Code, "00", StringComparison.OrdinalIgnoreCase))
            {
                await MarkDirectPayOsOrderAsFailedAsync(order, transactionLog, paidAt);
                order = await GetOrderByIdInternalAsync(order.Id);
                return MapToPayOsStatusDto(order, transactionLog);
            }

            if (verifiedData.Amount != decimal.ToInt64(transactionLog.Amount))
            {
                await MarkDirectPayOsOrderAsFailedAsync(order, transactionLog, paidAt);
                order = await GetOrderByIdInternalAsync(order.Id);
                return MapToPayOsStatusDto(order, transactionLog);
            }

            try
            {
                var package = await ValidatePackageAsync(order.PackageId);
                EnsurePayOsAmountSupported(package.Price);

                await ExpireOrdersAsync(order.StudentId);
                await EnsureStudentHasNoActiveOrderAsync(order.StudentId, order.Id);

                order.Status = OrderStatus.PAID;
                order.StartDate = paidAt;
                order.EndDate = paidAt.AddDays(package.DurationDays);
                order.PaidAt = paidAt;
                order.ExpiredAt = null;

                transactionLog.Status = PaymentStatus.SUCCESS.ToString();
                transactionLog.PaidAt = paidAt;

                _orderRepo.Update(order);
                _transactionLogRepo.Update(transactionLog);
                await UpsertOrderPaymentAsync(order.Id, transactionLog.Amount, PaymentStatus.SUCCESS, paidAt);
                await _orderRepo.SaveChangesAsync();
            }
            catch
            {
                await MarkDirectPayOsOrderAsFailedAsync(order, transactionLog, paidAt);
            }

            order = await GetOrderByIdInternalAsync(order.Id);
            return MapToPayOsStatusDto(order, transactionLog);
        }

        public async Task<OrderPayOsStatusDto> GetPayOsOrderStatusAsync(long orderCode)
        {
            if (orderCode <= 0)
                throw new Exception("OrderCode phải lớn hơn 0");

            var transactionLog = await GetPayOsOrderTransactionAsync(orderCode);
            var order = await GetOrderForTransactionAsync(transactionLog);

            if (order.Status == OrderStatus.PAID)
            {
                await ExpireOrdersAsync(order.StudentId);
                order = await GetOrderByIdInternalAsync(order.Id);
            }

            return MapToPayOsStatusDto(order, transactionLog);
        }

        public async Task<PagedResult<OrderDto>> SearchOrderAsync(
            string? status,
            long? guardianId,
            long? studentId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            var query = GetOrderQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                    throw new Exception($"Status '{status}' không hợp lệ.");

                query = query.Where(x => x.Status == orderStatus);
            }

            if (guardianId.HasValue)
                query = query.Where(x => x.GuardianId == guardianId.Value);

            if (studentId.HasValue)
                query = query.Where(x => x.StudentId == studentId.Value);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.CreatedAt.Date >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date;
                query = query.Where(x => x.CreatedAt.Date <= to);
            }

            var studentIds = await query.Select(x => x.StudentId).Distinct().ToListAsync();
            foreach (var currentStudentId in studentIds)
                await ExpireOrdersAsync(currentStudentId);

            query = GetOrderQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                Enum.TryParse<OrderStatus>(status, true, out var orderStatus);
                query = query.Where(x => x.Status == orderStatus);
            }

            if (guardianId.HasValue)
                query = query.Where(x => x.GuardianId == guardianId.Value);

            if (studentId.HasValue)
                query = query.Where(x => x.StudentId == studentId.Value);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.CreatedAt.Date >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date;
                query = query.Where(x => x.CreatedAt.Date <= to);
            }

            var totalItems = await query.CountAsync();

            var orders = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<OrderDto>
            {
                Items = orders.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<OrderDto> GetOrderByIdAsync(long id)
        {
            var order = await GetOrderByIdInternalAsync(id);

            await ExpireOrdersAsync(order.StudentId);

            order = await GetOrderByIdInternalAsync(id);
            return MapToDto(order);
        }

        public async Task<List<OrderDto>> GetOrdersByGuardianIdAsync(long guardianId)
        {
            await ValidateGuardianAsync(guardianId);

            var orders = await GetOrderQueryable()
                .Where(x => x.GuardianId == guardianId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            foreach (var studentId in orders.Select(x => x.StudentId).Distinct())
            {
                await ExpireOrdersAsync(studentId);
            }

            orders = await GetOrderQueryable()
                .Where(x => x.GuardianId == guardianId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto> CancelOrderAsync(long id, OrderCancelDto dto)
        {
            var order = await GetOrderByIdInternalAsync(id);
            var currentStatus = order.Status;

            if (currentStatus == OrderStatus.CANCELLED)
                throw new Exception("Order đã được hủy trước đó");

            await ExpireOrdersAsync(order.StudentId);
            order = await GetOrderByIdInternalAsync(id);
            currentStatus = order.Status;

            if (currentStatus == OrderStatus.EXPIRED)
                throw new Exception("Order đã hết hạn, không thể hủy");

            if (currentStatus == OrderStatus.PAID && dto.RefundToWallet)
            {
                var wallet = await FindOrCreateWalletAsync(order.GuardianId);
                var oldBalance = wallet.Balance;
                wallet.Balance += order.Package.Price;

                var transactionLog = new TransactionLog
                {
                    OrderId = order.Id,
                    Method = WalletMethod,
                    Amount = order.Package.Price,
                    Status = PaymentStatus.SUCCESS.ToString(),
                    PaidAt = DateTime.UtcNow,
                    OldBalance = oldBalance,
                    NewBalance = wallet.Balance,
                    Sender = SystemReceiver,
                    Receiver = order.Guardian.Email,
                    Description = BuildCancelDescription(order, dto.Reason),
                    Code = $"REFUND-{Guid.NewGuid():N}".ToUpperInvariant()
                };

                _walletRepo.Update(wallet);
                await _transactionLogRepo.AddAsync(transactionLog);
            }

            order.Status = OrderStatus.CANCELLED;
            order.ExpiredAt = null;

            if (currentStatus == OrderStatus.PAID)
            {
                order.EndDate = DateTime.UtcNow;
            }

            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();

            return MapToDto(await GetOrderByIdInternalAsync(id));
        }

        public async Task<OrderDto?> GetActiveOrderByStudentIdAsync(long studentId)
        {
            await ValidateStudentExistsAsync(studentId);
            await ExpireOrdersAsync(studentId);

            var order = await GetOrderQueryable()
                .Where(x => x.StudentId == studentId && x.Status == OrderStatus.PAID)
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefaultAsync();

            return order == null ? null : MapToDto(order);
        }

        private IQueryable<Order> GetOrderQueryable()
        {
            return _orderRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Student)
                .Include(x => x.BusRoute)
                .Include(x => x.Package);
        }

        private async Task<Order> GetOrderByIdInternalAsync(long id)
        {
            return await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Order không tồn tại");
        }

        private async Task ExpireOrdersAsync(long studentId)
        {
            var now = DateTime.UtcNow;
            var orders = await _orderRepo.Get()
                .Where(x =>
                    x.StudentId == studentId &&
                    x.Status == OrderStatus.PAID &&
                    x.EndDate.HasValue &&
                    x.EndDate.Value < now)
                .ToListAsync();

            if (!orders.Any())
                return;

            foreach (var order in orders)
            {
                order.Status = OrderStatus.EXPIRED;
                order.ExpiredAt = now;
                _orderRepo.Update(order);
            }

            await _orderRepo.SaveChangesAsync();
        }

        private async Task EnsureStudentHasNoActiveOrderAsync(long studentId, long? excludeOrderId = null)
        {
            var hasActiveOrder = await _orderRepo.Get()
                .AnyAsync(x =>
                    x.StudentId == studentId &&
                    x.Status == OrderStatus.PAID &&
                    x.EndDate.HasValue &&
                    x.EndDate.Value >= DateTime.UtcNow &&
                    (!excludeOrderId.HasValue || x.Id != excludeOrderId.Value));

            if (hasActiveOrder)
                throw new Exception("Student đang có gói còn hiệu lực");
        }

        private async Task<User> ValidateGuardianAsync(long guardianId)
        {
            if (guardianId <= 0)
                throw new Exception("GuardianId phải lớn hơn 0");

            var guardian = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == guardianId)
                ?? throw new Exception("Guardian không tồn tại");

            if (!string.Equals(guardian.Role.Name, "guardian", StringComparison.OrdinalIgnoreCase))
                throw new Exception("User được chọn không phải guardian");

            if (guardian.Status != AccountStatus.ACTIVE)
                throw new Exception("Guardian đang không hoạt động");

            return guardian;
        }

        private async Task<Student> ValidateStudentAsync(long studentId, long guardianId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == studentId)
                ?? throw new Exception("Student không tồn tại");

            if (student.GuardianId != guardianId)
                throw new Exception("Student không thuộc guardian này");

            return student;
        }

        private async Task ValidateStudentExistsAsync(long studentId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            var exists = await _studentRepo.Get()
                .AnyAsync(x => x.Id == studentId);

            if (!exists)
                throw new Exception("Student không tồn tại");
        }

        private async Task<Package> ValidatePackageAsync(long packageId)
        {
            if (packageId <= 0)
                throw new Exception("PackageId phải lớn hơn 0");

            var package = await _packageRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == packageId)
                ?? throw new Exception("Package không tồn tại");

            if (!string.Equals(package.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Package đang không hoạt động");

            if (package.DurationDays <= 0)
                throw new Exception("Package phải có DurationDays lớn hơn 0");

            if (package.Price <= 0)
                throw new Exception("Package phải có giá lớn hơn 0");

            if (package.RouteLimit <= 0)
                throw new Exception("Package phải có RouteLimit lớn hơn 0");

            return package;
        }

        private async Task<List<long>> ValidateSelectedRoutesAsync(List<long>? routeIds, long campusId, int routeLimit)
        {
            if (routeIds == null || !routeIds.Any())
                throw new Exception("Phải chọn tuyến cho gói");

            var normalizedRouteIds = routeIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (normalizedRouteIds.Count != routeLimit)
                throw new Exception($"Gói này yêu cầu chọn đúng {routeLimit} tuyến");

            var routes = await _busRouteRepo.Get()
                .Where(x => normalizedRouteIds.Contains(x.Id))
                .ToListAsync();

            if (routes.Count != normalizedRouteIds.Count)
                throw new Exception("Có tuyến không tồn tại");

            if (routes.Any(x => !x.IsEnabled))
                throw new Exception("Có tuyến đang không hoạt động");

            if (routes.Any(x => x.CampusId != campusId))
                throw new Exception("Tất cả tuyến phải thuộc cùng campus của học sinh");

            return normalizedRouteIds;
        }

        private static void EnsurePayOsAmountSupported(decimal amount)
        {
            if (decimal.Truncate(amount) != amount)
                throw new Exception("Giá gói thanh toán qua payOS phải là số nguyên VND");
        }

        private static bool IsPayOsUrlVerificationPing(WebhookData data)
        {
            return data.OrderCode == 123
                && data.Amount == 3000
                && string.Equals(data.Description, "VQRIO123", StringComparison.Ordinal);
        }

        private async Task<long> GeneratePayOsOrderCodeAsync()
        {
            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            while (await _transactionLogRepo.Get().AnyAsync(x => x.Code == orderCode.ToString()))
            {
                orderCode++;
            }

            return orderCode;
        }

        private async Task<TransactionLog> GetPayOsOrderTransactionAsync(long orderCode)
        {
            return await _transactionLogRepo.Get()
                .FirstOrDefaultAsync(x => x.Method == PayOsOrderMethod && x.Code == orderCode.ToString())
                ?? throw new Exception("Không tìm thấy giao dịch mua gói payOS");
        }

        private async Task<Wallet> FindOrCreateWalletAsync(long userId)
        {
            var wallet = await _walletRepo.Get()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (wallet != null)
                return wallet;

            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0
            };

            await _walletRepo.AddAsync(wallet);
            return wallet;
        }

        private async Task<Order> GetOrderForTransactionAsync(TransactionLog transactionLog)
        {
            if (!transactionLog.OrderId.HasValue)
                throw new Exception("Giao dịch payOS chưa gắn với order");

            return await GetOrderByIdInternalAsync(transactionLog.OrderId.Value);
        }

        private static string ResolveUrl(string? inputUrl, string configuredUrl, string fieldName)
        {
            var url = string.IsNullOrWhiteSpace(inputUrl) ? configuredUrl : inputUrl.Trim();

            if (string.IsNullOrWhiteSpace(url))
                throw new Exception($"{fieldName} của payOS chưa được cấu hình");

            return url;
        }

        private static string BuildDirectOrderDescription(long studentId)
        {
            return $"Mua goi HS{studentId}";
        }

        private static string BuildCancelDescription(Order order, string? reason)
        {
            var description = $"Hoàn tiền hủy order {order.Id} cho học sinh {order.Student.FullName}";

            if (!string.IsNullOrWhiteSpace(reason))
                description += $". Lý do: {reason.Trim()}";

            return description;
        }

        private async Task MarkDirectPayOsOrderAsFailedAsync(Order order, TransactionLog transactionLog, DateTime? paidAt)
        {
            order.Status = OrderStatus.CANCELLED;
            order.StartDate = null;
            order.EndDate = null;
            order.PaidAt = null;
            order.ExpiredAt = null;

            transactionLog.Status = PaymentStatus.FAILED.ToString();
            transactionLog.PaidAt = paidAt;

            _orderRepo.Update(order);
            _transactionLogRepo.Update(transactionLog);
            await UpsertOrderPaymentAsync(order.Id, transactionLog.Amount, PaymentStatus.FAILED, paidAt);
            await _orderRepo.SaveChangesAsync();
        }

        private async Task UpsertOrderPaymentAsync(long orderId, decimal amount, PaymentStatus status, DateTime? paidAt)
        {
            var payment = await _paymentRepo.Get()
                .FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (payment == null)
            {
                payment = new Payment
                {
                    OrderId = orderId,
                    Method = PayOsPaymentMethod,
                    Amount = amount,
                    Status = status,
                    PaidAt = paidAt
                };

                await _paymentRepo.AddAsync(payment);
                return;
            }

            payment.Method = PayOsPaymentMethod;
            payment.Amount = amount;
            payment.Status = status;
            payment.PaidAt = paidAt;
            _paymentRepo.Update(payment);
        }

        private static DateTime ParseTransactionDateTime(string? transactionDateTime)
        {
            if (DateTime.TryParse(transactionDateTime, out var parsedDateTime))
                return parsedDateTime;

            return DateTime.UtcNow;
        }

        private static string JoinSelectedRouteIds(IEnumerable<long> routeIds)
        {
            return string.Join(",", routeIds);
        }

        private static List<long> ParseSelectedRouteIds(string? selectedRouteIds)
        {
            if (string.IsNullOrWhiteSpace(selectedRouteIds))
                return new List<long>();

            var normalizedValue = selectedRouteIds
                .Trim()
                .TrimStart('[')
                .TrimEnd(']');

            if (string.IsNullOrWhiteSpace(normalizedValue))
                return new List<long>();

            return normalizedValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => long.TryParse(x, out var routeId) ? routeId : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                GuardianId = order.GuardianId,
                GuardianName = order.Guardian.FullName ?? string.Empty,
                StudentId = order.StudentId,
                StudentName = order.Student.FullName,
                BusRouteId = order.BusRouteId,
                BusRouteName = order.BusRoute?.Name,
                SelectedRouteIds = ParseSelectedRouteIds(order.SelectedRouteIds),
                PackageId = order.PackageId,
                PackageName = order.Package.Name,
                PackagePrice = order.Package.Price,
                DurationDays = order.Package.DurationDays,
                PackageRouteLimit = order.Package.RouteLimit,
                Status = order.Status.ToString(),
                StartDate = order.StartDate,
                EndDate = order.EndDate,
                PaidAt = order.PaidAt,
                ExpiredAt = order.ExpiredAt,
                CreatedAt = order.CreatedAt
            };
        }

        private static OrderPayOsStatusDto MapToPayOsStatusDto(Order order, TransactionLog transactionLog)
        {
            return new OrderPayOsStatusDto
            {
                OrderId = order.Id,
                GuardianId = order.GuardianId,
                StudentId = order.StudentId,
                PackageId = order.PackageId,
                PackageName = order.Package.Name,
                SelectedRouteIds = ParseSelectedRouteIds(order.SelectedRouteIds),
                PackageRouteLimit = order.Package.RouteLimit,
                OrderCode = long.TryParse(transactionLog.Code, out var orderCode) ? orderCode : 0,
                Amount = transactionLog.Amount,
                OrderStatus = order.Status.ToString(),
                TransactionStatus = transactionLog.Status,
                PaidAt = transactionLog.PaidAt ?? order.PaidAt,
                StartDate = order.StartDate,
                EndDate = order.EndDate,
                CreatedAt = order.CreatedAt
            };
        }
    }
}
