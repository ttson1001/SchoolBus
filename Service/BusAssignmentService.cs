using BE_API.Dto.BusAssignment;
using BE_API.Dto.BusSchedule;
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
        private readonly IRepository<BusSchedule> _busScheduleRepo;
        private readonly IRepository<User> _userRepo;

        public BusAssignmentService(
            IRepository<BusAssignment> busAssignmentRepo,
            IRepository<BusSchedule> busScheduleRepo,
            IRepository<User> userRepo)
        {
            _busAssignmentRepo = busAssignmentRepo;
            _busScheduleRepo = busScheduleRepo;
            _userRepo = userRepo;
        }

        public async Task<PagedResult<BusAssignmentDto>> SearchAsync(
            string? keyword,
            long? busScheduleId,
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
                    x.BusSchedule.Bus.LicensePlate.ToLower().Contains(keyword) ||
                    (x.BusSchedule.Bus.BusNumber != null && x.BusSchedule.Bus.BusNumber.ToLower().Contains(keyword)) ||
                    ((x.Driver.FullName ?? x.Driver.Email).ToLower().Contains(keyword)) ||
                    ((x.Teacher.FullName ?? x.Teacher.Email).ToLower().Contains(keyword)) ||
                    x.BusSchedule.Route.Name.ToLower().Contains(keyword) ||
                    x.BusSchedule.Route.Campus.Name.ToLower().Contains(keyword));
            }

            if (busScheduleId.HasValue)
                query = query.Where(x => x.BusScheduleId == busScheduleId.Value);

            if (driverId.HasValue)
                query = query.Where(x => x.DriverId == driverId.Value);

            if (teacherId.HasValue)
                query = query.Where(x => x.TeacherId == teacherId.Value);

            if (activeDate.HasValue)
            {
                var selectedDate = activeDate.Value.Date;
                var nextDate = selectedDate.AddDays(1);
                query = query.Where(x => x.ActiveDate.HasValue && x.ActiveDate.Value >= selectedDate && x.ActiveDate.Value < nextDate);
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
            var busSchedule = await ValidateBusScheduleAsync(dto.BusScheduleId, normalizedActiveDate);
            var driver = await ValidateUserByRoleAsync(dto.DriverId, "driver");
            var teacher = await ValidateUserByRoleAsync(dto.TeacherId, "teacher");

            ValidateDriverTeacherPair(driver.Id, teacher.Id);
            await EnsureAssignmentNotDuplicatedAsync(busSchedule.Id, driver.Id, teacher.Id, normalizedActiveDate, null);
            await EnsureDriverTeacherAvailabilityAsync(busSchedule, driver.Id, teacher.Id, normalizedActiveDate, null);

            var assignment = new BusAssignment
            {
                BusScheduleId = busSchedule.Id,
                DriverId = driver.Id,
                TeacherId = teacher.Id,
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

            var busScheduleId = dto.BusScheduleId ?? assignment.BusScheduleId;
            var driverId = dto.DriverId ?? assignment.DriverId;
            var teacherId = dto.TeacherId ?? assignment.TeacherId;
            var normalizedActiveDate = dto.ActiveDate.HasValue ? dto.ActiveDate.Value.Date : assignment.ActiveDate?.Date;

            var busSchedule = await ValidateBusScheduleAsync(busScheduleId, normalizedActiveDate);
            var driver = await ValidateUserByRoleAsync(driverId, "driver");
            var teacher = await ValidateUserByRoleAsync(teacherId, "teacher");

            ValidateDriverTeacherPair(driver.Id, teacher.Id);
            await EnsureAssignmentNotDuplicatedAsync(busSchedule.Id, driver.Id, teacher.Id, normalizedActiveDate, id);
            await EnsureDriverTeacherAvailabilityAsync(busSchedule, driver.Id, teacher.Id, normalizedActiveDate, id);

            assignment.BusScheduleId = busSchedule.Id;
            assignment.DriverId = driver.Id;
            assignment.TeacherId = teacher.Id;
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
                .Include(x => x.BusSchedule)
                .ThenInclude(x => x.Bus)
                .Include(x => x.BusSchedule)
                .ThenInclude(x => x.Route)
                .ThenInclude(x => x.Campus)
                .Include(x => x.Driver)
                .ThenInclude(x => x.Role)
                .Include(x => x.Teacher)
                .ThenInclude(x => x.Role);
        }

        private async Task<BusSchedule> ValidateBusScheduleAsync(long busScheduleId, DateTime? activeDate)
        {
            if (busScheduleId <= 0)
                throw new Exception("BusScheduleId phải lớn hơn 0");

            var schedule = await _busScheduleRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Route)
                .ThenInclude(x => x.Campus)
                .FirstOrDefaultAsync(x => x.Id == busScheduleId)
                ?? throw new Exception("Bus schedule không tồn tại");

            if (!schedule.IsActive)
                throw new Exception("Bus schedule đang không hoạt động");

            if (!string.Equals(schedule.Bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Bus '{schedule.Bus.LicensePlate}' đang không hoạt động");

            if (!schedule.Route.IsEnabled)
                throw new Exception("Bus route đang không hoạt động");

            if (!schedule.Route.Campus.IsActive)
                throw new Exception($"Campus '{schedule.Route.Campus.Name}' đang không hoạt động");

            if (activeDate.HasValue)
            {
                var normalizedActiveDate = activeDate.Value.Date;
                var actualDayOfWeek = (int)normalizedActiveDate.DayOfWeek;

                if (schedule.StartDate.Date > normalizedActiveDate ||
                    (schedule.EndDate.HasValue && schedule.EndDate.Value.Date < normalizedActiveDate))
                {
                    throw new Exception("ActiveDate không nằm trong thời gian áp dụng của bus schedule");
                }

                if (schedule.DayOfWeek != actualDayOfWeek)
                {
                    throw new Exception(
                        $"ActiveDate {normalizedActiveDate:yyyy-MM-dd} là {GetDayOfWeekName(actualDayOfWeek)} " +
                        $"nhưng bus schedule đang là {GetDayOfWeekName(schedule.DayOfWeek)}");
                }
            }

            return schedule;
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

        private static void ValidateDriverTeacherPair(long driverId, long teacherId)
        {
            if (driverId == teacherId)
                throw new Exception("Driver và teacher không được là cùng một người");
        }

        private async Task EnsureAssignmentNotDuplicatedAsync(
            long busScheduleId,
            long driverId,
            long teacherId,
            DateTime? activeDate,
            long? excludedId)
        {
            var start = activeDate?.Date;
            var end = start?.AddDays(1);

            var duplicated = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.BusScheduleId == busScheduleId &&
                    x.DriverId == driverId &&
                    x.TeacherId == teacherId &&
                    x.ActiveDate >= start &&
                    x.ActiveDate < end &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (duplicated)
                throw new Exception("Bus assignment đã tồn tại với đầy đủ thông tin trùng nhau");
        }

        private async Task EnsureDriverTeacherAvailabilityAsync(
            BusSchedule busSchedule,
            long driverId,
            long teacherId,
            DateTime? activeDate,
            long? excludedId)
        {
            var start = activeDate?.Date;
            var end = start?.AddDays(1);
            var targetBusId = busSchedule.BusId;

            var sameBusSameDate = await _busAssignmentRepo.Get()
                .Include(x => x.BusSchedule)
                .AnyAsync(x =>
                    x.BusSchedule.BusId == targetBusId &&
                    x.ActiveDate >= start &&
                    x.ActiveDate < end &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameBusSameDate)
                throw new Exception("Bus đã được phân công trong ngày này");

            var sameDriverSameDate = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.DriverId == driverId &&
                    x.ActiveDate >= start &&
                    x.ActiveDate < end &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameDriverSameDate)
                throw new Exception("Driver đã được phân công cho bus khác trong ngày này");

            var sameTeacherSameDate = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.TeacherId == teacherId &&
                    x.ActiveDate >= start &&
                    x.ActiveDate < end &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameTeacherSameDate)
                throw new Exception("Teacher đã được phân công cho bus khác trong ngày này");
        }

        private static string Capitalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? value
                : char.ToUpperInvariant(value[0]) + value[1..];
        }

        private static string GetDayOfWeekName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                0 => "Chủ nhật",
                1 => "Thứ hai",
                2 => "Thứ ba",
                3 => "Thứ tư",
                4 => "Thứ năm",
                5 => "Thứ sáu",
                6 => "Thứ bảy",
                _ => $"Ngày {dayOfWeek}"
            };
        }

        private static BusAssignmentDto MapToDto(BusAssignment assignment)
        {
            return new BusAssignmentDto
            {
                Id = assignment.Id,
                BusScheduleId = assignment.BusScheduleId,
                BusSchedule = MapToBusScheduleDto(assignment.BusSchedule),
                DriverId = assignment.DriverId,
                Driver = MapToUserDto(assignment.Driver),
                TeacherId = assignment.TeacherId,
                Teacher = MapToUserDto(assignment.Teacher),
                ActiveDate = assignment.ActiveDate
            };
        }

        private static BusScheduleDto MapToBusScheduleDto(BusSchedule schedule)
        {
            return new BusScheduleDto
            {
                Id = schedule.Id,
                BusId = schedule.BusId,
                BusLabel = schedule.Bus.BusNumber ?? schedule.Bus.LicensePlate,
                RouteId = schedule.RouteId,
                RouteName = schedule.Route.Name,
                CampusId = schedule.Route.CampusId,
                CampusName = schedule.Route.Campus?.Name ?? string.Empty,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                DayOfWeek = schedule.DayOfWeek,
                ShiftType = schedule.ShiftType,
                IsActive = schedule.IsActive
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
    }
}
