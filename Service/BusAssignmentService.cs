using BE_API.Dto.BusAssignment;
using BE_API.Dto.Bus;
using BE_API.Dto.BusRoute;
using BE_API.Dto.Common;
using BE_API.Dto.User;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusAssignmentService : IBusAssignmentService
    {
        private readonly IRepository<BusAssignment> _busAssignmentRepo;
        private readonly IRepository<Bus> _busRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<BusRoute> _routeRepo;

        public BusAssignmentService(
            IRepository<BusAssignment> busAssignmentRepo,
            IRepository<Bus> busRepo,
            IRepository<User> userRepo,
            IRepository<BusRoute> routeRepo)
        {
            _busAssignmentRepo = busAssignmentRepo;
            _busRepo = busRepo;
            _userRepo = userRepo;
            _routeRepo = routeRepo;
        }

        public async Task<PagedResult<BusAssignmentDto>> SearchAsync(
            string? keyword,
            long? busId,
            long? routeId,
            long? driverId,
            long? teacherId,
            DateTime? activeDate,
            int page,
            int pageSize)
        {
            var query = GetQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.Bus.LicensePlate.ToLower().Contains(keyword) ||
                    (x.Bus.BusNumber != null && x.Bus.BusNumber.ToLower().Contains(keyword)) ||
                    ((x.Driver.FullName ?? x.Driver.Email).ToLower().Contains(keyword)) ||
                    ((x.Teacher.FullName ?? x.Teacher.Email).ToLower().Contains(keyword)) ||
                    x.Route.Name.ToLower().Contains(keyword) ||
                    x.Route.Campus.Name.ToLower().Contains(keyword));
            }

            if (busId.HasValue)
                query = query.Where(x => x.BusId == busId.Value);

            if (routeId.HasValue)
                query = query.Where(x => x.RouteId == routeId.Value);

            if (driverId.HasValue)
                query = query.Where(x => x.DriverId == driverId.Value);

            if (teacherId.HasValue)
                query = query.Where(x => x.TeacherId == teacherId.Value);

            if (activeDate.HasValue)
            {
                var selectedDate = activeDate.Value.Date;
                query = query.Where(x => x.ActiveDate.HasValue && x.ActiveDate.Value.Date == selectedDate);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.ActiveDate)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BusAssignmentDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<BusAssignmentDto> GetByIdAsync(long id)
        {
            var assignment = await GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus assignment không tồn tại");

            return MapToDto(assignment);
        }

        public async Task<BusAssignmentDto> CreateAsync(BusAssignmentCreateDto dto)
        {
            var normalizedActiveDate = dto.ActiveDate?.Date;
            var bus = await ValidateBusAsync(dto.BusId);
            var driver = await ValidateUserByRoleAsync(dto.DriverId, "driver");
            var teacher = await ValidateUserByRoleAsync(dto.TeacherId, "teacher");
            var route = await ValidateRouteAsync(dto.RouteId);

            ValidateDriverTeacherPair(driver.Id, teacher.Id);
            await EnsureAssignmentNotDuplicatedAsync(bus.Id, route.Id, driver.Id, teacher.Id, normalizedActiveDate, null);
            await EnsureDriverTeacherAvailabilityAsync(bus.Id, driver.Id, teacher.Id, normalizedActiveDate, null);

            var assignment = new BusAssignment
            {
                BusId = bus.Id,
                DriverId = driver.Id,
                TeacherId = teacher.Id,
                RouteId = route.Id,
                ActiveDate = normalizedActiveDate
            };

            await _busAssignmentRepo.AddAsync(assignment);
            await _busAssignmentRepo.SaveChangesAsync();

            return await GetByIdAsync(assignment.Id);
        }

        public async Task<BusAssignmentDto> UpdateAsync(long id, BusAssignmentUpdateDto dto)
        {
            var assignment = await _busAssignmentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus assignment không tồn tại");

            var busId = dto.BusId ?? assignment.BusId;
            var driverId = dto.DriverId ?? assignment.DriverId;
            var teacherId = dto.TeacherId ?? assignment.TeacherId;
            var routeId = dto.RouteId ?? assignment.RouteId;
            var normalizedActiveDate = dto.ActiveDate.HasValue ? dto.ActiveDate.Value.Date : assignment.ActiveDate?.Date;

            var bus = await ValidateBusAsync(busId);
            var driver = await ValidateUserByRoleAsync(driverId, "driver");
            var teacher = await ValidateUserByRoleAsync(teacherId, "teacher");
            var route = await ValidateRouteAsync(routeId);

            ValidateDriverTeacherPair(driver.Id, teacher.Id);
            await EnsureAssignmentNotDuplicatedAsync(bus.Id, route.Id, driver.Id, teacher.Id, normalizedActiveDate, id);
            await EnsureDriverTeacherAvailabilityAsync(bus.Id, driver.Id, teacher.Id, normalizedActiveDate, id);

            assignment.BusId = bus.Id;
            assignment.DriverId = driver.Id;
            assignment.TeacherId = teacher.Id;
            assignment.RouteId = route.Id;
            assignment.ActiveDate = normalizedActiveDate;

            _busAssignmentRepo.Update(assignment);
            await _busAssignmentRepo.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(long id)
        {
            var assignment = await _busAssignmentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus assignment không tồn tại");

            _busAssignmentRepo.Delete(assignment);
            await _busAssignmentRepo.SaveChangesAsync();
        }

        private IQueryable<BusAssignment> GetQueryable()
        {
            return _busAssignmentRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Driver)
                .ThenInclude(x => x.Role)
                .Include(x => x.Teacher)
                .ThenInclude(x => x.Role)
                .Include(x => x.Route)
                .ThenInclude(x => x.Campus)
                .Include(x => x.Route)
                .ThenInclude(x => x.Stations)
                .ThenInclude(x => x.Station);
        }

        private async Task<Bus> ValidateBusAsync(long busId)
        {
            if (busId <= 0)
                throw new Exception("BusId phải lớn hơn 0");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == busId)
                ?? throw new Exception("Bus không tồn tại");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Bus '{bus.LicensePlate}' đang không hoạt động");

            return bus;
        }

        private async Task<User> ValidateUserByRoleAsync(long userId, string roleName)
        {
            if (userId <= 0)
                throw new Exception($"{roleName}Id phải lớn hơn 0");

            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception($"{Capitalize(roleName)} không tồn tại");

            if (!string.Equals(user.Role.Name, roleName, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"User được chọn không phải {roleName}");

            if (user.Status != AccountStatus.ACTIVE)
                throw new Exception($"{Capitalize(roleName)} đang không hoạt động");

            if (string.Equals(roleName, "driver", StringComparison.OrdinalIgnoreCase) &&
                user.DriverLicenseExpiryDate.HasValue &&
                user.DriverLicenseExpiryDate.Value.Date <= DateTime.UtcNow.Date)
            {
                throw new Exception("Driver đã hết hạn bằng lái");
            }

            return user;
        }

        private async Task<BusRoute> ValidateRouteAsync(long routeId)
        {
            if (routeId <= 0)
                throw new Exception("RouteId phải lớn hơn 0");

            var route = await _routeRepo.Get()
                .Include(x => x.Campus)
                .FirstOrDefaultAsync(x => x.Id == routeId)
                ?? throw new Exception("Bus route không tồn tại");

            if (!route.IsEnabled)
                throw new Exception("Bus route đang không hoạt động");

            if (!route.Campus.IsActive)
                throw new Exception($"Campus '{route.Campus.Name}' đang không hoạt động");

            return route;
        }

        private static void ValidateDriverTeacherPair(long driverId, long teacherId)
        {
            if (driverId == teacherId)
                throw new Exception("Driver và teacher không được là cùng một người");
        }

        private async Task EnsureAssignmentNotDuplicatedAsync(
            long busId,
            long routeId,
            long driverId,
            long teacherId,
            DateTime? activeDate,
            long? excludedId)
        {
            var duplicated = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.BusId == busId &&
                    x.RouteId == routeId &&
                    x.DriverId == driverId &&
                    x.TeacherId == teacherId &&
                    ((!x.ActiveDate.HasValue && !activeDate.HasValue) ||
                     (x.ActiveDate.HasValue && activeDate.HasValue && x.ActiveDate.Value.Date == activeDate.Value.Date)) &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (duplicated)
                throw new Exception("Bus assignment đã tồn tại với đầy đủ thông tin trùng nhau");
        }

        private async Task EnsureDriverTeacherAvailabilityAsync(
            long busId,
            long driverId,
            long teacherId,
            DateTime? activeDate,
            long? excludedId)
        {
            var sameBusSameDate = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.BusId == busId &&
                    IsSameDate(x.ActiveDate, activeDate) &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameBusSameDate)
                throw new Exception("Bus đã được phân công trong ngày này");

            var sameDriverSameDate = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.DriverId == driverId &&
                    IsSameDate(x.ActiveDate, activeDate) &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameDriverSameDate)
                throw new Exception("Driver đã được phân công cho bus khác trong ngày này");

            var sameTeacherSameDate = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.TeacherId == teacherId &&
                    IsSameDate(x.ActiveDate, activeDate) &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameTeacherSameDate)
                throw new Exception("Teacher đã được phân công cho bus khác trong ngày này");
        }

        private static bool IsSameDate(DateTime? left, DateTime? right)
        {
            if (!left.HasValue && !right.HasValue)
                return true;

            if (left.HasValue && right.HasValue)
                return left.Value.Date == right.Value.Date;

            return false;
        }

        private static string Capitalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? value
                : char.ToUpperInvariant(value[0]) + value[1..];
        }

        private static BusAssignmentDto MapToDto(BusAssignment assignment)
        {
            return new BusAssignmentDto
            {
                Id = assignment.Id,
                BusId = assignment.BusId,
                Bus = MapToBusDto(assignment.Bus),
                DriverId = assignment.DriverId,
                Driver = MapToUserDto(assignment.Driver),
                TeacherId = assignment.TeacherId,
                Teacher = MapToUserDto(assignment.Teacher),
                RouteId = assignment.RouteId,
                Route = MapToBusRouteDto(assignment.Route),
                ActiveDate = assignment.ActiveDate
            };
        }

        private static BusDto MapToBusDto(Bus bus)
        {
            return new BusDto
            {
                Id = bus.Id,
                LicensePlate = bus.LicensePlate,
                Capacity = bus.Capacity,
                Status = bus.Status,
                BusNumber = bus.BusNumber,
                ImageUrl = bus.ImageUrl,
                Color = bus.Color,
                BusType = bus.BusType
            };
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                DeviceToken = user.DeviceToken,
                DriverLicenseNumber = user.DriverLicenseNumber,
                DriverLicenseClass = user.DriverLicenseClass,
                DriverLicenseExpiryDate = user.DriverLicenseExpiryDate,
                RoleName = user.Role?.Name ?? string.Empty,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            };
        }

        private static BusRouteDto MapToBusRouteDto(BusRoute route)
        {
            return new BusRouteDto
            {
                Id = route.Id,
                Name = route.Name,
                IsEnabled = route.IsEnabled,
                CampusId = route.CampusId,
                CampusName = route.Campus?.Name ?? string.Empty,
                Stations = route.Stations
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => new BusRouteStationDto
                    {
                        Id = x.Station?.Id ?? x.StationId,
                        Name = x.Station?.Name ?? string.Empty,
                        Address = x.Station?.Address,
                        Description = x.Station?.Description,
                        Latitude = x.Station?.Latitude,
                        Longitude = x.Station?.Longitude,
                        IsEnabled = x.Station?.IsEnabled ?? false,
                        OrderIndex = x.OrderIndex
                    })
                    .ToList()
            };
        }
    }
}
