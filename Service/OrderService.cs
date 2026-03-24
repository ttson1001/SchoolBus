using BE_API.Dto.Order;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Package> _packageRepo;
        private readonly IRepository<Wallet> _walletRepo;
        private readonly IRepository<Payment> _paymentRepo;
        private readonly IRepository<TransactionLog> _transactionLogRepo;

        public OrderService(
            IRepository<Order> orderRepo,
            IRepository<User> userRepo,
            IRepository<Student> studentRepo,
            IRepository<Package> packageRepo,
            IRepository<Wallet> walletRepo,
            IRepository<Payment> paymentRepo,
            IRepository<TransactionLog> transactionLogRepo)
        {
            _orderRepo = orderRepo;
            _userRepo = userRepo;
            _studentRepo = studentRepo;
            _packageRepo = packageRepo;
            _walletRepo = walletRepo;
            _paymentRepo = paymentRepo;
            _transactionLogRepo = transactionLogRepo;
        }

        public async Task<OrderDto> CreateOrderAsync(OrderCreateDto dto)
        {
            var guardian = await ValidateGuardianAsync(dto.GuardianId);
            var student = await ValidateStudentAsync(dto.StudentId, dto.GuardianId);
            var package = await ValidatePackageAsync(dto.PackageId);

            await ExpireOrdersAsync(dto.StudentId);

            var hasActiveOrder = await _orderRepo.Get()
                .AnyAsync(x =>
                    x.StudentId == dto.StudentId &&
                    x.Status == OrderStatus.PAID &&
                    x.EndDate.HasValue &&
                    x.EndDate.Value >= DateTime.UtcNow);

            if (hasActiveOrder)
                throw new Exception("Student dang co goi con hieu luc");

            var wallet = await _walletRepo.Get()
                .FirstOrDefaultAsync(x => x.UserId == guardian.Id);

            if (wallet == null)
                throw new Exception("Guardian chua co vi. Vui long nap tien truoc");

            if (wallet.Balance < package.Price)
                throw new Exception("So du khong du de mua goi");

            var now = DateTime.UtcNow;
            var oldBalance = wallet.Balance;
            var endDate = now.AddDays(package.DurationDays);

            wallet.Balance -= package.Price;
            _walletRepo.Update(wallet);

            var order = new Order
            {
                GuardianId = guardian.Id,
                StudentId = student.Id,
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
                Method = "WALLET",
                Amount = package.Price,
                Status = PaymentStatus.SUCCESS,
                PaidAt = now
            };

            await _paymentRepo.AddAsync(payment);

            var transactionLog = new TransactionLog
            {
                Order = order,
                Method = "WALLET",
                Amount = package.Price,
                Status = PaymentStatus.SUCCESS.ToString(),
                PaidAt = now,
                OldBalance = oldBalance,
                NewBalance = wallet.Balance,
                Sender = guardian.Email,
                Receiver = "SYSTEM",
                Description = $"Thanh toan goi {package.Name} bang vi",
                Code = $"WALLET-{Guid.NewGuid():N}".ToUpperInvariant()
            };

            await _transactionLogRepo.AddAsync(transactionLog);
            await _orderRepo.SaveChangesAsync();

            var createdOrder = await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == order.Id)
                ?? throw new Exception("Order khong ton tai");

            return MapToDto(createdOrder);
        }

        public async Task<OrderDto> GetOrderByIdAsync(long id)
        {
            var order = await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Order khong ton tai");

            await ExpireOrdersAsync(order.StudentId);

            order = await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Order khong ton tai");

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

        private async Task<User> ValidateGuardianAsync(long guardianId)
        {
            if (guardianId <= 0)
                throw new Exception("GuardianId phai lon hon 0");

            var guardian = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == guardianId)
                ?? throw new Exception("Guardian khong ton tai");

            if (!string.Equals(guardian.Role.Name, "guardian", StringComparison.OrdinalIgnoreCase))
                throw new Exception("User duoc chon khong phai guardian");

            if (guardian.Status != AccountStatus.ACTIVE)
                throw new Exception("Guardian dang khong hoat dong");

            return guardian;
        }

        private async Task<Student> ValidateStudentAsync(long studentId, long guardianId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phai lon hon 0");

            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == studentId)
                ?? throw new Exception("Student khong ton tai");

            if (student.GuardianId != guardianId)
                throw new Exception("Student khong thuoc guardian nay");

            return student;
        }

        private async Task ValidateStudentExistsAsync(long studentId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phai lon hon 0");

            var exists = await _studentRepo.Get()
                .AnyAsync(x => x.Id == studentId);

            if (!exists)
                throw new Exception("Student khong ton tai");
        }

        private async Task<Package> ValidatePackageAsync(long packageId)
        {
            if (packageId <= 0)
                throw new Exception("PackageId phai lon hon 0");

            var package = await _packageRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == packageId)
                ?? throw new Exception("Package khong ton tai");

            if (!string.Equals(package.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Package dang khong hoat dong");

            if (package.DurationDays <= 0)
                throw new Exception("Package phai co DurationDays lon hon 0");

            if (package.Price <= 0)
                throw new Exception("Package phai co gia lon hon 0");

            return package;
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
                PackageId = order.PackageId,
                PackageName = order.Package.Name,
                PackagePrice = order.Package.Price,
                DurationDays = order.Package.DurationDays,
                Status = order.Status.ToString(),
                StartDate = order.StartDate,
                EndDate = order.EndDate,
                PaidAt = order.PaidAt,
                ExpiredAt = order.ExpiredAt,
                CreatedAt = order.CreatedAt
            };
        }
    }
}
