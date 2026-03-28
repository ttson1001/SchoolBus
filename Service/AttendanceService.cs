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
        private readonly IRepository<BusRouteStation> _routeStationRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public AttendanceService(
            IRepository<Attendance> attendanceRepo,
            IRepository<Student> studentRepo,
            IRepository<Bus> busRepo,
            IRepository<StudentBusAssignment> assignmentRepo,
            IRepository<BusRouteStation> routeStationRepo,
            IRepository<Notification> notificationRepo,
            IFirebaseNotificationService firebaseNotificationService)
        {
            _attendanceRepo = attendanceRepo;
            _studentRepo = studentRepo;
            _busRepo = busRepo;
            _assignmentRepo = assignmentRepo;
            _routeStationRepo = routeStationRepo;
            _notificationRepo = notificationRepo;
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task<PagedResult<AttendanceDto>> SearchAttendanceAsync(string? keyword, DateTime? date, int page, int pageSize)
        {
            var query = GetAttendanceQueryable();

            if (date.HasValue)
            {
                var selectedDate = date.Value.Date;
                query = query.Where(x => x.Date.Date == selectedDate);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Student.FullName.ToLower().Contains(keyword) ||
                    x.Bus.LicensePlate.ToLower().Contains(keyword) ||
                    (x.CheckInStation != null && x.CheckInStation.Name.ToLower().Contains(keyword)) ||
                    (x.CheckOutStation != null && x.CheckOutStation.Name.ToLower().Contains(keyword)));
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
            var attendance = await GetAttendanceQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Attendance khong ton tai");

            return MapToDto(attendance);
        }

        public async Task<AttendanceDto> ManualCheckInAsync(AttendanceManualDto dto)
        {
            var validation = await ValidateManualAttendanceAsync(dto);

            var attendance = await GetAttendanceQueryable()
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.Date.Date == validation.AttendanceDate);

            if (attendance != null && attendance.CheckInTime.HasValue)
                throw new Exception("Hoc sinh da check in trong ngay nay");

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    StudentId = validation.Student.Id,
                    BusId = validation.Bus.Id,
                    Date = validation.AttendanceDate,
                    Method = AttendanceMethod.MANUAL,
                    Status = AttendanceStatus.PRESENT,
                    CheckInTime = validation.CheckTime,
                    CheckInStationId = validation.Station.Id
                };

                await _attendanceRepo.AddAsync(attendance);
                await _attendanceRepo.SaveChangesAsync();

                attendance = await GetAttendanceQueryable()
                    .FirstOrDefaultAsync(x => x.Id == attendance.Id)
                    ?? throw new Exception("Attendance khong ton tai");
            }
            else
            {
                attendance.BusId = validation.Bus.Id;
                attendance.CheckInTime = validation.CheckTime;
                attendance.CheckInStationId = validation.Station.Id;
                attendance.Method = AttendanceMethod.MANUAL;
                attendance.Status = AttendanceStatus.PRESENT;

                _attendanceRepo.Update(attendance);
                await _attendanceRepo.SaveChangesAsync();

                attendance.Student = validation.Student;
                attendance.Bus = validation.Bus;
                attendance.CheckInStation = validation.Station;
            }

            await CreateGuardianNotificationAsync(
                validation.Student,
                validation.Bus,
                validation.Assignment.Route.Name,
                validation.AttendanceDate,
                validation.CheckTime,
                "BOARDING",
                $"Hoc sinh {validation.Student.FullName} da len xe {validation.Bus.LicensePlate}" +
                $"{FormatRouteSuffix(validation.Assignment.Route.Name)}" +
                $"{FormatStationSuffix(validation.Station.Name)} luc {FormatTime(validation.CheckTime)} ngay {validation.AttendanceDate:dd/MM/yyyy}.");

            return MapToDto(attendance);
        }

        public async Task<AttendanceDto> ManualCheckOutAsync(AttendanceManualDto dto)
        {
            var validation = await ValidateManualAttendanceAsync(dto);

            var attendance = await GetAttendanceQueryable()
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.Date.Date == validation.AttendanceDate)
                ?? throw new Exception("Khong tim thay attendance de check out");

            if (!attendance.CheckInTime.HasValue)
                throw new Exception("Hoc sinh chua check in");

            if (attendance.CheckOutTime.HasValue)
                throw new Exception("Hoc sinh da check out trong ngay nay");

            attendance.BusId = validation.Bus.Id;
            attendance.CheckOutTime = validation.CheckTime;
            attendance.CheckOutStationId = validation.Station.Id;
            attendance.Method = AttendanceMethod.MANUAL;
            attendance.Status = AttendanceStatus.PRESENT;

            _attendanceRepo.Update(attendance);
            await _attendanceRepo.SaveChangesAsync();

            attendance.Student = validation.Student;
            attendance.Bus = validation.Bus;
            attendance.CheckOutStation = validation.Station;

            await CreateGuardianNotificationAsync(
                validation.Student,
                validation.Bus,
                validation.Assignment.Route.Name,
                validation.AttendanceDate,
                validation.CheckTime,
                "ALIGHTING",
                $"Hoc sinh {validation.Student.FullName} da xuong xe {validation.Bus.LicensePlate}" +
                $"{FormatRouteSuffix(validation.Assignment.Route.Name)}" +
                $"{FormatStationSuffix(validation.Station.Name)} luc {FormatTime(validation.CheckTime)} ngay {validation.AttendanceDate:dd/MM/yyyy}.");

            if (validation.Assignment.DropOffStationId.HasValue &&
                validation.Assignment.DropOffStationId.Value != validation.Station.Id)
            {
                var expectedStationName = validation.Assignment.DropOffStation?.Name ?? "khong ro";

                await CreateGuardianNotificationAsync(
                    validation.Student,
                    validation.Bus,
                    validation.Assignment.Route.Name,
                    validation.AttendanceDate,
                    validation.CheckTime,
                    "WRONG_DROPOFF",
                    $"Canh bao: Hoc sinh {validation.Student.FullName} da xuong xe {validation.Bus.LicensePlate}" +
                    $"{FormatRouteSuffix(validation.Assignment.Route.Name)} tai diem {validation.Station.Name}, " +
                    $"khong dung diem da dang ky {expectedStationName} luc {FormatTime(validation.CheckTime)} ngay {validation.AttendanceDate:dd/MM/yyyy}.");
            }

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

        private IQueryable<Attendance> GetAttendanceQueryable()
        {
            return _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .Include(x => x.CheckInStation)
                .Include(x => x.CheckOutStation);
        }

        private async Task<ManualAttendanceValidationResult> ValidateManualAttendanceAsync(AttendanceManualDto dto)
        {
            if (dto.StationId <= 0)
                throw new Exception("StationId phai lon hon 0");

            var student = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .FirstOrDefaultAsync(x => x.Id == dto.StudentId)
                ?? throw new Exception("Student khong ton tai");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == dto.BusId)
                ?? throw new Exception("Bus khong ton tai");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bus khong o trang thai hoat dong");

            var attendanceDate = (dto.Date ?? DateTime.Now).Date;
            var checkTime = dto.Time ?? DateTime.Now.TimeOfDay;

            var assignmentQuery = _assignmentRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.PickupStation)
                .Include(x => x.DropOffStation)
                .Where(x => x.StudentId == dto.StudentId && x.BusId == dto.BusId);

            var assignment = await assignmentQuery
                .FirstOrDefaultAsync(x => x.RideDate.HasValue && x.RideDate.Value.Date == attendanceDate);

            assignment ??= await assignmentQuery
                .FirstOrDefaultAsync(x => !x.RideDate.HasValue);

            if (assignment == null)
                throw new Exception("Hoc sinh chua duoc set diem don tra cho bus nay trong ngay da chon");

            var routeStation = await _routeStationRepo.Get()
                .Where(x => x.RouteId == assignment.RouteId && x.StationId == dto.StationId)
                .Include(x => x.Station)
                .FirstOrDefaultAsync()
                ?? throw new Exception("Bus station khong thuoc route cua hoc sinh");

            if (!routeStation.Station.IsEnabled)
                throw new Exception($"Bus station '{routeStation.Station.Name}' dang khong hoat dong");

            return new ManualAttendanceValidationResult
            {
                Student = student,
                Bus = bus,
                Assignment = assignment,
                Station = routeStation.Station,
                AttendanceDate = attendanceDate,
                CheckTime = checkTime
            };
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

            await _firebaseNotificationService.SendAsync(
                student.Guardian?.DeviceToken,
                BuildPushTitle(type),
                message,
                new Dictionary<string, string>
                {
                    ["type"] = type,
                    ["studentId"] = student.Id.ToString(),
                    ["guardianId"] = student.GuardianId.ToString(),
                    ["busId"] = bus.Id.ToString(),
                    ["busLicensePlate"] = bus.LicensePlate,
                    ["routeName"] = routeName,
                    ["attendanceDate"] = attendanceDate.ToString("yyyy-MM-dd"),
                    ["checkTime"] = FormatTime(checkTime)
                });
        }

        private static string FormatRouteSuffix(string? routeName)
        {
            return string.IsNullOrWhiteSpace(routeName) ? string.Empty : $" tren tuyen {routeName}";
        }

        private static string FormatStationSuffix(string? stationName)
        {
            return string.IsNullOrWhiteSpace(stationName) ? string.Empty : $" tai diem {stationName}";
        }

        private static string FormatTime(TimeSpan time)
        {
            return time.ToString(@"hh\:mm");
        }

        private static string BuildPushTitle(string type)
        {
            return type switch
            {
                "BOARDING" => "Học sinh đã lên xe",
                "ALIGHTING" => "Học sinh đã xuống xe",
                "WRONG_DROPOFF" => "Cảnh báo xuống sai điểm",
                _ => "Thông báo SchoolBus"
            };
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
                CheckInStationId = attendance.CheckInStationId,
                CheckInStationName = attendance.CheckInStation?.Name,
                CheckOutStationId = attendance.CheckOutStationId,
                CheckOutStationName = attendance.CheckOutStation?.Name,
                Method = attendance.Method.ToString(),
                Status = attendance.Status.ToString()
            };
        }

        private class ManualAttendanceValidationResult
        {
            public Student Student { get; set; } = null!;
            public Bus Bus { get; set; } = null!;
            public StudentBusAssignment Assignment { get; set; } = null!;
            public BusStation Station { get; set; } = null!;
            public DateTime AttendanceDate { get; set; }
            public TimeSpan CheckTime { get; set; }
        }
    }
}
