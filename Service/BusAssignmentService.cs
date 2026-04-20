using BE_API.Common;
using BE_API.Dto.Bus;
using BE_API.Dto.BusAssignment;
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
        private readonly IAppTime _appTime;

        public BusAssignmentService(
            IRepository<BusAssignment> busAssignmentRepo,
            IRepository<Bus> busRepo,
            IRepository<User> userRepo,
            IAppTime appTime)
        {
            _busAssignmentRepo = busAssignmentRepo;
            _busRepo = busRepo;
            _userRepo = userRepo;
            _appTime = appTime;
        }

        public async Task<PagedResult<BusAssignmentDto>> SearchAsync(
            string? keyword,
            long? busId,
            long? driverId,
            long? teacherId,
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
                    ((x.Teacher.FullName ?? x.Teacher.Email).ToLower().Contains(keyword)));
            }

            if (busId.HasValue)
                query = query.Where(x => x.BusId == busId.Value);

            if (driverId.HasValue)
                query = query.Where(x => x.DriverId == driverId.Value);

            if (teacherId.HasValue)
                query = query.Where(x => x.TeacherId == teacherId.Value);

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
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
            var bus = await ValidateBusAsync(dto.BusId);
            var driver = await ValidateUserByRoleAsync(dto.DriverId, "driver");
            var teacher = await ValidateUserByRoleAsync(dto.TeacherId, "teacher");
            var existingAssignment = await FindByBusIdAsync(bus.Id);

            ValidateDriverTeacherPair(driver.Id, teacher.Id);
            await EnsureStaffAvailabilityAsync(driver.Id, teacher.Id, existingAssignment?.Id);

            if (existingAssignment != null)
            {
                existingAssignment.DriverId = driver.Id;
                existingAssignment.TeacherId = teacher.Id;

                _busAssignmentRepo.Update(existingAssignment);
                await _busAssignmentRepo.SaveChangesAsync();

                return await GetByIdAsync(existingAssignment.Id);
            }

            var assignment = new BusAssignment
            {
                BusId = bus.Id,
                DriverId = driver.Id,
                TeacherId = teacher.Id
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

            var bus = await ValidateBusAsync(busId);
            var driver = await ValidateUserByRoleAsync(driverId, "driver");
            var teacher = await ValidateUserByRoleAsync(teacherId, "teacher");
            var existingAssignment = await FindByBusIdAsync(bus.Id);

            ValidateDriverTeacherPair(driver.Id, teacher.Id);
            await EnsureStaffAvailabilityAsync(driver.Id, teacher.Id, existingAssignment?.Id == id ? id : existingAssignment?.Id);

            if (existingAssignment != null && existingAssignment.Id != id)
            {
                existingAssignment.DriverId = driver.Id;
                existingAssignment.TeacherId = teacher.Id;

                _busAssignmentRepo.Update(existingAssignment);
                _busAssignmentRepo.Delete(assignment);
                await _busAssignmentRepo.SaveChangesAsync();

                return await GetByIdAsync(existingAssignment.Id);
            }

            assignment.BusId = bus.Id;
            assignment.DriverId = driver.Id;
            assignment.TeacherId = teacher.Id;

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
                .ThenInclude(x => x.Role);
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
                user.DriverLicenseExpiryDate.Value.Date <= _appTime.TodayDate)
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

        private async Task EnsureStaffAvailabilityAsync(long driverId, long teacherId, long? excludedId)
        {
            var sameDriver = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.DriverId == driverId &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameDriver)
                throw new Exception("Driver đã được phân công cho xe khác");

            var sameTeacher = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.TeacherId == teacherId &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (sameTeacher)
                throw new Exception("Teacher đã được phân công cho xe khác");
        }

        private async Task<BusAssignment?> FindByBusIdAsync(long busId)
        {
            return await _busAssignmentRepo.Get()
                .FirstOrDefaultAsync(x => x.BusId == busId);
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
                Teacher = MapToUserDto(assignment.Teacher)
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
    }
}
