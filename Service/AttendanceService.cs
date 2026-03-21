using BE_API.Dto.Attendance;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IRepository<Attendance> _attendanceRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Bus> _busRepo;
        private readonly IRepository<StudentBusAssignment> _assignmentRepo;
        private readonly IRepository<Notification> _notificationRepo;

        public AttendanceService(
            IRepository<Attendance> attendanceRepo,
            IRepository<Student> studentRepo,
            IRepository<Bus> busRepo,
            IRepository<StudentBusAssignment> assignmentRepo,
            IRepository<Notification> notificationRepo)
        {
            _attendanceRepo = attendanceRepo;
            _studentRepo = studentRepo;
            _busRepo = busRepo;
            _assignmentRepo = assignmentRepo;
            _notificationRepo = notificationRepo;
        }

        public async Task<PagedResult<AttendanceDto>> SearchAttendanceAsync(string? keyword, DateTime? date, int page, int pageSize)
        {
            var query = _attendanceRepo.Get();

            if (date.HasValue)
            {
                var selectedDate = date.Value.Date;
                query = query.Where(x => x.Date.Date == selectedDate)
                    .Include(x => x.Student)
                    .Include(x => x.Bus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                        x.Student.FullName.ToLower().Contains(keyword) ||
                        x.Bus.LicensePlate.ToLower().Contains(keyword))
                    .Include(x => x.Student)
                    .Include(x => x.Bus);
            }

            var totalItems = await query.CountAsync();

            var attendances = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AttendanceDto>
            {
                Items = attendances.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AttendanceDto> GetAttendanceByIdAsync(long id)
        {
            var attendance = await _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Attendance khong ton tai");

            return MapToDto(attendance);
        }

        public async Task<AttendanceDto> ManualCheckInAsync(AttendanceManualDto dto)
        {
            var (student, bus, assignment, attendanceDate, checkTime) = await ValidateManualAttendanceAsync(dto);

            var attendance = await _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.Date.Date == attendanceDate);

            if (attendance != null && attendance.CheckInTime.HasValue)
                throw new Exception("Hoc sinh da check in trong ngay nay");

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    StudentId = student.Id,
                    BusId = bus.Id,
                    Date = attendanceDate,
                    Method = AttendanceMethod.MANUAL,
                    Status = AttendanceStatus.PRESENT,
                    CheckInTime = checkTime
                };

                await _attendanceRepo.AddAsync(attendance);
                await _attendanceRepo.SaveChangesAsync();
            }
            else
            {
                attendance.BusId = bus.Id;
                attendance.CheckInTime = checkTime;
                attendance.Method = AttendanceMethod.MANUAL;
                attendance.Status = AttendanceStatus.PRESENT;

                _attendanceRepo.Update(attendance);
                await _attendanceRepo.SaveChangesAsync();
            }

            attendance.Student = student;
            attendance.Bus = bus;

            await CreateGuardianNotificationAsync(
                student,
                bus,
                assignment.Route.Name,
                attendanceDate,
                checkTime,
                "BOARDING",
                $"Hoc sinh {student.FullName} da len xe {bus.LicensePlate}" +
                $"{FormatRouteSuffix(assignment.Route.Name)} luc {FormatTime(checkTime)} ngay {attendanceDate:dd/MM/yyyy}.");

            return MapToDto(attendance);
        }

        public async Task<AttendanceDto> ManualCheckOutAsync(AttendanceManualDto dto)
        {
            var (student, bus, assignment, attendanceDate, checkTime) = await ValidateManualAttendanceAsync(dto);

            var attendance = await _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.Date.Date == attendanceDate)
                ?? throw new Exception("Khong tim thay attendance de check out");

            if (!attendance.CheckInTime.HasValue)
                throw new Exception("Hoc sinh chua check in");

            if (attendance.CheckOutTime.HasValue)
                throw new Exception("Hoc sinh da check out trong ngay nay");

            attendance.BusId = bus.Id;
            attendance.Bus = bus;
            attendance.Student = student;
            attendance.CheckOutTime = checkTime;
            attendance.Method = AttendanceMethod.MANUAL;
            attendance.Status = AttendanceStatus.PRESENT;

            _attendanceRepo.Update(attendance);
            await _attendanceRepo.SaveChangesAsync();

            await CreateGuardianNotificationAsync(
                student,
                bus,
                assignment.Route.Name,
                attendanceDate,
                checkTime,
                "ALIGHTING",
                $"Hoc sinh {student.FullName} da xuong xe {bus.LicensePlate}" +
                $"{FormatRouteSuffix(assignment.Route.Name)} luc {FormatTime(checkTime)} ngay {attendanceDate:dd/MM/yyyy}.");

            return MapToDto(attendance);
        }

        public async Task DeleteAttendanceAsync(long id)
        {
            var attendance = await _attendanceRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Attendance khong ton tai");

            _attendanceRepo.Delete(attendance);
            await _attendanceRepo.SaveChangesAsync();
        }

        private async Task<(Student student, Bus bus, StudentBusAssignment assignment, DateTime attendanceDate, TimeSpan checkTime)> ValidateManualAttendanceAsync(AttendanceManualDto dto)
        {
            var student = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .FirstOrDefaultAsync(x => x.Id == dto.StudentId)
                ?? throw new Exception("Student khong ton tai");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == dto.BusId)
                ?? throw new Exception("Bus khong ton tai");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bus khong o trang thai hoat dong");

            var assignment = await _assignmentRepo.Get()
                .Include(x => x.Route)
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.BusId == dto.BusId)
                ?? throw new Exception("Hoc sinh chua duoc gan vao bus nay");

            var attendanceDate = (dto.Date ?? DateTime.Now).Date;
            var checkTime = dto.Time ?? DateTime.Now.TimeOfDay;

            return (student, bus, assignment, attendanceDate, checkTime);
        }

        private async Task CreateGuardianNotificationAsync(
            Student student,
            Bus bus,
            string routeName,
            DateTime attendanceDate,
            TimeSpan checkTime,
            string type,
            string message)
        {
            if (student.GuardianId <= 0)
                return;

            var duplicatedNotification = await _notificationRepo.Get()
                .AnyAsync(x =>
                    x.UserId == student.GuardianId &&
                    x.Type == type &&
                    x.Message == message &&
                    x.CreatedAt.Date == attendanceDate.Date);

            if (duplicatedNotification)
                return;

            var notification = new Notification
            {
                UserId = student.GuardianId,
                Type = type,
                Message = message,
                CreatedAt = attendanceDate.Date.Add(checkTime)
            };

            await _notificationRepo.AddAsync(notification);
            await _notificationRepo.SaveChangesAsync();
        }

        private static string FormatRouteSuffix(string? routeName)
        {
            return string.IsNullOrWhiteSpace(routeName) ? string.Empty : $" tren tuyen {routeName}";
        }

        private static string FormatTime(TimeSpan time)
        {
            return time.ToString(@"hh\:mm");
        }

        private static AttendanceDto MapToDto(Attendance attendance)
        {
            return new AttendanceDto
            {
                Id = attendance.Id,
                StudentId = attendance.StudentId,
                StudentName = attendance.Student.FullName,
                BusId = attendance.BusId,
                BusLicensePlate = attendance.Bus.LicensePlate,
                Date = attendance.Date,
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                Method = attendance.Method.ToString(),
                Status = attendance.Status.ToString()
            };
        }
    }
}
