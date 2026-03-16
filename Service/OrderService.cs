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
        private readonly IRepository<BusRoute> _busRouteRepo;

        public OrderService(
            IRepository<Order> orderRepo,
            IRepository<User> userRepo,
            IRepository<Student> studentRepo,
            IRepository<Package> packageRepo,
            IRepository<BusRoute> busRouteRepo)
        {
            _orderRepo = orderRepo;
            _userRepo = userRepo;
            _studentRepo = studentRepo;
            _packageRepo = packageRepo;
            _busRouteRepo = busRouteRepo;
        }

        public async Task<OrderDto> CreateOrderAsync(OrderCreateDto dto)
        {
            var guardian = await ValidateGuardianAsync(dto.GuardianId);
            var student = await ValidateStudentAsync(dto.StudentId, dto.GuardianId);
            var package = await ValidatePackageAsync(dto.PackageId);
            var busRoute = await ValidateBusRouteAsync(dto.BusRouteId);

            await ExpireOrdersAsync(dto.StudentId);

            var hasActiveOrder = await _orderRepo.Get()
                .AnyAsync(x =>
                    x.StudentId == dto.StudentId &&
                    x.Status == OrderStatus.PAID &&
                    x.EndDate.HasValue &&
                    x.EndDate.Value >= DateTime.UtcNow);

            if (hasActiveOrder)
                throw new Exception("Student đang có gói còn hiệu lực");

            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(package.DurationDays);

            var order = new Order
            {
                GuardianId = guardian.Id,
                StudentId = student.Id,
                BusRouteId = busRoute.Id,
                PackageId = package.Id,
                Status = OrderStatus.PAID,
                StartDate = startDate,
                EndDate = endDate,
                PaidAt = startDate,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            var createdOrder = await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == order.Id)
                ?? throw new Exception("Order không tồn tại");

            return MapToDto(createdOrder);
        }

        public async Task<OrderDto> GetOrderByIdAsync(long id)
        {
            var order = await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Order không tồn tại");

            await ExpireOrdersAsync(order.StudentId);

            order = await GetOrderQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Order không tồn tại");

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

            return package;
        }

        private async Task<BusRoute> ValidateBusRouteAsync(long busRouteId)
        {
            if (busRouteId <= 0)
                throw new Exception("BusRouteId phải lớn hơn 0");

            var busRoute = await _busRouteRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == busRouteId)
                ?? throw new Exception("BusRoute không tồn tại");

            if (!busRoute.IsEnabled)
                throw new Exception("BusRoute đang không hoạt động");

            return busRoute;
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
                BusRouteName = order.BusRoute.Name,
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
