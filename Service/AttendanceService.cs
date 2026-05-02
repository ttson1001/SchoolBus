using BE_API.Common;
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
        private readonly IRepository<BusRun> _busRunRepo;
        private readonly IRepository<BusRunStudent> _busRunStudentRepo;
        private readonly IRepository<BusRouteStation> _routeStationRepo;
        private readonly IRepository<Order> _orderRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IAppTime _appTime;

        public AttendanceService(
            IRepository<Attendance> attendanceRepo,
            IRepository<Student> studentRepo,
            IRepository<Bus> busRepo,
            IRepository<BusRun> busRunRepo,
            IRepository<BusRunStudent> busRunStudentRepo,
            IRepository<BusRouteStation> routeStationRepo,
            IRepository<Order> orderRepo,
            IRepository<Notification> notificationRepo,
            IFirebaseNotificationService firebaseNotificationService,
            IAppTime appTime)
        {
            _attendanceRepo = attendanceRepo;
            _studentRepo = studentRepo;
            _busRepo = busRepo;
            _busRunRepo = busRunRepo;
            _busRunStudentRepo = busRunStudentRepo;
            _routeStationRepo = routeStationRepo;
            _orderRepo = orderRepo;
            _notificationRepo = notificationRepo;
            _firebaseNotificationService = firebaseNotificationService;
            _appTime = appTime;
        }

        public async Task<PagedResult<AttendanceDto>> SearchAttendanceAsync(string? keyword, DateTime? date, long? campusId, long? busId, long? studentId, long? guardianId, string? status, int page, int pageSize)
        {
            var query = GetAttendanceQueryable();

            if (date.HasValue)
            {
                var selectedDate = date.Value.Date;
                query = query.Where(x => x.Date.Date == selectedDate);
            }

            if (campusId.HasValue)
                query = query.Where(x => x.Student.CampusId == campusId.Value);

            if (busId.HasValue)
                query = query.Where(x => x.BusId == busId.Value);

            if (studentId.HasValue)
                query = query.Where(x => x.StudentId == studentId.Value);

            if (guardianId.HasValue)
                query = query.Where(x => x.Student.GuardianId == guardianId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<AttendanceStatus>(status, true, out var attendanceStatus))
                    throw new Exception($"Status '{status}' không hợp lệ.");

                query = query.Where(x => x.Status == attendanceStatus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Student.FullName.ToLower().Contains(keyword) ||
                    x.Bus.LicensePlate.ToLower().Contains(keyword) ||
                    x.Student.Campus.Name.ToLower().Contains(keyword) ||
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

        public async Task<List<AttendanceDto>> GetAttendanceByStudentIdAsync(long studentId, DateTime? fromDate, DateTime? toDate)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == studentId)
                ?? throw new Exception("Student không tồn tại");

            var query = GetAttendanceQueryable()
                .Where(x => x.StudentId == student.Id);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.Date.Date >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date;
                query = query.Where(x => x.Date.Date <= to);
            }

            var attendances = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return attendances.Select(MapToDto).ToList();
        }

        public async Task<AttendanceDto> GetAttendanceByIdAsync(long id)
        {
            var attendance = await GetAttendanceQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Attendance không tồn tại");

            return MapToDto(attendance);
        }

        public async Task<List<AttendanceOnBusStudentDto>> GetStudentsOnBusAsync(long busId, DateTime? date, long? busRunId = null)
        {
            if (busId <= 0)
                throw new Exception("BusId phải lớn hơn 0");

            var selectedDate = _appTime.GetRideCalendarDate(date);
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == busId)
                ?? throw new Exception("Bus không tồn tại");

            var busRuns = await ResolveBusRunsAsync(busId, selectedDate, busRunId);
            var assignedStudentIds = await _busRunStudentRepo.Get()
                .Where(x => busRuns.Contains(x.BusRunId))
                .Select(x => x.StudentId)
                .Distinct()
                .ToListAsync();

            var attendances = await GetAttendanceQueryable()
                .Where(x =>
                    x.BusId == busId &&
                    x.Date.Date == selectedDate.Date &&
                    assignedStudentIds.Contains(x.StudentId) &&
                    x.Status == AttendanceStatus.CHECKED_IN &&
                    x.CheckInTime.HasValue &&
                    !x.CheckOutTime.HasValue)
                .OrderBy(x => x.CheckInTime)
                .ThenBy(x => x.Student.StudentCode)
                .ThenBy(x => x.Student.FullName)
                .ToListAsync();

            return attendances.Select(MapToOnBusDto).ToList();
        }

        public async Task<List<AttendanceBusStudentStatusDto>> GetBusStudentStatusesAsync(long busId, DateTime? date, long? busRunId = null)
        {
            if (busId <= 0)
                throw new Exception("BusId phải lớn hơn 0");

            var selectedDate = _appTime.GetRideCalendarDate(date);
            var busRuns = await ResolveBusRunsAsync(busId, selectedDate, busRunId);

            var runStudents = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .ThenInclude(x => x.Guardian)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Where(x => busRuns.Contains(x.BusRunId))
                .ToListAsync();

            var attendances = await GetAttendanceQueryable()
                .Where(x => x.BusId == busId && x.Date.Date == selectedDate.Date)
                .ToListAsync();

            return runStudents
                .GroupBy(x => x.StudentId)
                .Select(group =>
                {
                    var runStudent = group
                        .OrderBy(x => x.Booking.StartTime)
                        .ThenBy(x => x.BookingId)
                        .First();

                    var attendance = attendances
                        .Where(x => x.StudentId == runStudent.StudentId)
                        .OrderByDescending(x => x.CheckInTime ?? TimeSpan.MinValue)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();

                    var isOnBus = attendance != null &&
                                  attendance.Status == AttendanceStatus.CHECKED_IN &&
                                  attendance.CheckInTime.HasValue &&
                                  !attendance.CheckOutTime.HasValue;

                    return new AttendanceBusStudentStatusDto
                    {
                        StudentId = runStudent.StudentId,
                        StudentCode = runStudent.Student.StudentCode,
                        StudentName = runStudent.Student.FullName,
                        StudentAvatarUrl = runStudent.Student.AvatarUrl,
                        GuardianId = runStudent.Student.GuardianId,
                        GuardianName = runStudent.Student.Guardian?.FullName ?? string.Empty,
                        GuardianPhone = runStudent.Student.Guardian?.Phone,
                        BookingId = runStudent.BookingId,
                        StationId = runStudent.Booking.StationId,
                        StationName = runStudent.Booking.Station.Name,
                        AttendanceId = attendance?.Id,
                        CheckInTime = attendance?.CheckInTime,
                        CheckOutTime = attendance?.CheckOutTime,
                        IsOnBus = isOnBus
                    };
                })
                .OrderByDescending(x => x.IsOnBus)
                .ThenBy(x => x.StationId)
                .ThenBy(x => x.StudentCode)
                .ThenBy(x => x.StudentName)
                .ToList();
        }

        private async Task<List<long>> ResolveBusRunsAsync(long busId, DateTime selectedDate, long? busRunId)
        {
            if (busRunId.HasValue && busRunId.Value <= 0)
                throw new Exception("BusRunId phải lớn hơn 0");

            var query = _busRunRepo.Get()
                .Where(x => x.BusId == busId && x.ServiceDate.Date == selectedDate.Date);

            if (busRunId.HasValue)
                query = query.Where(x => x.Id == busRunId.Value);

            var busRuns = await query
                .Select(x => x.Id)
                .ToListAsync();

            if (!busRuns.Any())
            {
                if (busRunId.HasValue)
                    throw new Exception("BusRun không tồn tại hoặc không thuộc bus/ngày đã chọn");

                throw new Exception("Bus này chưa có lịch chạy thực tế trong ngày đã chọn");
            }

            return busRuns;
        }

        public async Task<AttendanceDto> ManualCheckInAsync(AttendanceManualDto dto)
        {
            var validation = await ValidateManualAttendanceAsync(dto);
            var note = await BuildAttendanceNoteAsync(validation.Student.Id, validation.AttendanceDate);
            note = AppendOperationalNote(note, validation.OperationalNote);

            var attendance = await GetAttendanceQueryable()
                .Where(x => x.StudentId == dto.StudentId && x.Date.Date == validation.AttendanceDate)
                .OrderByDescending(x => x.CheckInTime ?? TimeSpan.MinValue)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (attendance != null && attendance.CheckInTime.HasValue && !attendance.CheckOutTime.HasValue)
                throw new Exception("Học sinh đã check in, chỉ có thể check out");

            attendance = new Attendance
            {
                StudentId = validation.Student.Id,
                BusId = validation.Bus.Id,
                Date = validation.AttendanceDate,
                Method = AttendanceMethod.MANUAL,
                Status = AttendanceStatus.CHECKED_IN,
                CheckInTime = validation.CheckTime,
                CheckInStationId = validation.Station.Id,
                CheckInImageUrl = NormalizeImageUrl(dto.ImageUrl),
                Note = note
            };

            await _attendanceRepo.AddAsync(attendance);
            await _attendanceRepo.SaveChangesAsync();

            attendance = await GetAttendanceQueryable()
                .FirstOrDefaultAsync(x => x.Id == attendance.Id)
                ?? throw new Exception("Attendance không tồn tại");

            await CreateGuardianNotificationAsync(
                validation.Student,
                validation.Bus,
                validation.RouteName,
                validation.AttendanceDate,
                validation.CheckTime,
                "BOARDING",
                $"Hoc sinh {validation.Student.FullName} da len xe {validation.Bus.LicensePlate}" +
                $"{FormatRouteSuffix(validation.RouteName)}" +
                $"{FormatStationSuffix(validation.Station.Name)} luc {FormatTime(validation.CheckTime)} ngay {validation.AttendanceDate:dd/MM/yyyy}.");

            return MapToDto(attendance);
        }

        public async Task<AttendanceDto> ManualCheckOutAsync(AttendanceManualDto dto)
        {
            var validation = await ValidateManualAttendanceAsync(dto);
            var note = await BuildAttendanceNoteAsync(validation.Student.Id, validation.AttendanceDate);
            note = AppendOperationalNote(note, validation.OperationalNote);

            var attendance = await GetAttendanceQueryable()
                .Where(x =>
                    x.StudentId == dto.StudentId &&
                    x.Date.Date == validation.AttendanceDate &&
                    x.CheckInTime.HasValue &&
                    !x.CheckOutTime.HasValue)
                .OrderByDescending(x => x.CheckInTime ?? TimeSpan.MinValue)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync()
                ?? throw new Exception("Không tìm thấy attendance để check out");

            if (!attendance.CheckInTime.HasValue)
                throw new Exception("Học sinh chưa check in");

            if (attendance.CheckOutTime.HasValue)
                throw new Exception("Học sinh đã check out trong ngày này");

            attendance.BusId = validation.Bus.Id;
            attendance.CheckOutTime = validation.CheckTime;
            attendance.CheckOutStationId = validation.Station.Id;
            attendance.CheckOutImageUrl = NormalizeImageUrl(dto.ImageUrl);
            attendance.Method = AttendanceMethod.MANUAL;
            attendance.Status = AttendanceStatus.CHECKED_OUT;
            attendance.Note = note;

            _attendanceRepo.Update(attendance);
            await _attendanceRepo.SaveChangesAsync();

            attendance.Student = validation.Student;
            attendance.Bus = validation.Bus;
            attendance.CheckOutStation = validation.Station;

            await CreateGuardianNotificationAsync(
                validation.Student,
                validation.Bus,
                validation.RouteName,
                validation.AttendanceDate,
                validation.CheckTime,
                "ALIGHTING",
                $"Hoc sinh {validation.Student.FullName} da xuong xe {validation.Bus.LicensePlate}" +
                $"{FormatRouteSuffix(validation.RouteName)}" +
                $"{FormatStationSuffix(validation.Station.Name)} luc {FormatTime(validation.CheckTime)} ngay {validation.AttendanceDate:dd/MM/yyyy}.");

            if (validation.ExpectedDropOffStationId.HasValue &&
                validation.ExpectedDropOffStationId.Value != validation.Station.Id)
            {
                var expectedStationName = validation.ExpectedDropOffStationName ?? "không rõ";

                await CreateGuardianNotificationAsync(
                    validation.Student,
                    validation.Bus,
                    validation.RouteName,
                    validation.AttendanceDate,
                    validation.CheckTime,
                    "WRONG_DROPOFF",
                    $"Cảnh báo: Học sinh {validation.Student.FullName} đã xuống xe {validation.Bus.LicensePlate}" +
                    $"{FormatRouteSuffix(validation.RouteName)} tại điểm {validation.Station.Name}, " +
                    $"không đúng điểm đã đăng ký {expectedStationName} lúc {FormatTime(validation.CheckTime)} ngày {validation.AttendanceDate:dd/MM/yyyy}.");
            }

            return MapToDto(attendance);
        }

        public async Task DeleteAttendanceAsync(long id)
        {
            var attendance = await _attendanceRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Attendance không tồn tại");

            _attendanceRepo.Delete(attendance);
            await _attendanceRepo.SaveChangesAsync();
        }

        private IQueryable<Attendance> GetAttendanceQueryable()
        {
            return _attendanceRepo.Get()
                .Include(x => x.Student)
                .ThenInclude(x => x.Guardian)
                .Include(x => x.Student)
                .ThenInclude(x => x.Campus)
                .Include(x => x.Bus)
                .Include(x => x.CheckInStation)
                .Include(x => x.CheckOutStation);
        }

        private async Task<ManualAttendanceValidationResult> ValidateManualAttendanceAsync(AttendanceManualDto dto)
        {
            if (dto.StationId <= 0)
                throw new Exception("StationId phải lớn hơn 0");

            var student = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .FirstOrDefaultAsync(x => x.Id == dto.StudentId)
                ?? throw new Exception("Student không tồn tại");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == dto.BusId)
                ?? throw new Exception("Bus không tồn tại");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bus không ở trạng thái hoạt động");

            var attendanceDate = _appTime.GetRideCalendarDate(dto.Date);
            var checkTime = dto.Time ?? _appTime.GetTimeOfDay();

            var actualBusRun = await ResolveAttendanceRunAsync(dto.BusId, attendanceDate, checkTime);
            var candidateRunStudents = await _busRunStudentRepo.Get()
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Include(x => x.BusRun)
                .ThenInclude(x => x.Bus)
                .Where(x =>
                    x.StudentId == dto.StudentId &&
                    x.Booking.RouteId == actualBusRun.RouteId &&
                    x.Booking.ServiceDate.Date == attendanceDate.Date)
                .OrderByDescending(x => x.Booking.StartTime)
                .ToListAsync();

            var runStudent = candidateRunStudents
                .FirstOrDefault(x => IsEligibleAttendanceTransferWindow(x.Booking.StartTime, actualBusRun.StartTime));

            if (runStudent == null)
                throw new Exception("Học sinh không nằm trong danh sách booking của tuyến này ở khung giờ đã chọn");

            var isCheckingInOnDifferentBus = runStudent.BusRunId != actualBusRun.Id;
            if (isCheckingInOnDifferentBus)
            {
                var currentStudentCount = await _busRunStudentRepo.Get()
                    .CountAsync(x => x.BusRunId == actualBusRun.Id);

                if (currentStudentCount >= actualBusRun.UsableCapacity)
                    throw new Exception("Xe này đã hết chỗ, không thể điểm danh thêm học sinh");
            }

            var routeStation = await _routeStationRepo.Get()
                .Where(x => x.RouteId == actualBusRun.RouteId && x.StationId == dto.StationId)
                .Include(x => x.Station)
                .FirstOrDefaultAsync()
                ?? throw new Exception("Bus station không thuộc route của bus trong ngày đã chọn");

            if (!routeStation.Station.IsEnabled)
                throw new Exception($"Bus station '{routeStation.Station.Name}' đang không hoạt động");

            return new ManualAttendanceValidationResult
            {
                Student = student,
                Bus = bus,
                RouteName = actualBusRun.Route.Name,
                ExpectedDropOffStationId = runStudent.Booking.StationId,
                ExpectedDropOffStationName = runStudent.Booking.Station?.Name,
                Station = routeStation.Station,
                AttendanceDate = attendanceDate,
                CheckTime = checkTime,
                ActualBusRunId = actualBusRun.Id,
                SourceBusRunStudent = runStudent,
                OperationalNote = BuildOperationalAttendanceNote(actualBusRun, runStudent)
            };
        }

        private async Task<BusRun> ResolveAttendanceRunAsync(long busId, DateTime attendanceDate, TimeSpan checkTime)
        {
            var runs = await _busRunRepo.Get()
                .Include(x => x.Route)
                .Where(x =>
                    x.BusId == busId &&
                    x.ServiceDate.Date == attendanceDate.Date)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.RunOrder)
                .ToListAsync();

            if (!runs.Any())
                throw new Exception("Bus này chưa có lịch chạy thực tế trong ngày đã chọn");

            for (var i = 0; i < runs.Count; i++)
            {
                var currentRun = runs[i];
                var nextStartTime = i < runs.Count - 1 ? runs[i + 1].StartTime : (TimeSpan?)null;

                if (IsTimeWithinBusRun(checkTime, currentRun.StartTime, nextStartTime))
                    return currentRun;
            }

            return runs.LastOrDefault(x => x.StartTime <= checkTime) ?? runs.First();
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
            return string.IsNullOrWhiteSpace(routeName) ? string.Empty : $" trên tuyến {routeName}";
        }

        private static string FormatStationSuffix(string? stationName)
        {
            return string.IsNullOrWhiteSpace(stationName) ? string.Empty : $" tại điểm {stationName}";
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

        private async Task<string> BuildAttendanceNoteAsync(long studentId, DateTime attendanceDate)
        {
            var hasActiveOrder = await _orderRepo.Get()
                .AnyAsync(x =>
                    x.StudentId == studentId &&
                    x.Status == OrderStatus.PAID &&
                    x.StartDate.HasValue &&
                    x.EndDate.HasValue &&
                    x.StartDate.Value.Date <= attendanceDate.Date &&
                    x.EndDate.Value.Date >= attendanceDate.Date);

            if (hasActiveOrder)
                return "Học sinh có gói còn hiệu lực";

            var hasAnyOrder = await _orderRepo.Get()
                .AnyAsync(x => x.StudentId == studentId);

            if (hasAnyOrder)
                return "Gói đã hết hạn nhưng vẫn được đi lần này";

            return "Học sinh chưa có gói nhưng vẫn được đi lần này";
        }

        private static string AppendOperationalNote(string baseNote, string? operationalNote)
        {
            if (string.IsNullOrWhiteSpace(operationalNote))
                return baseNote;

            return $"{baseNote}. {operationalNote}";
        }

        private static string? BuildOperationalAttendanceNote(BusRun actualBusRun, BusRunStudent runStudent)
        {
            if (runStudent.BusRunId == actualBusRun.Id)
                return null;

            var assignedBusLicensePlate = runStudent.BusRun?.Bus?.LicensePlate;
            var actualRunType = string.Equals(actualBusRun.Status, "BACKUP", StringComparison.OrdinalIgnoreCase)
                ? "xe backup"
                : actualBusRun.StartTime > runStudent.Booking.StartTime
                    ? "xe giờ sau"
                    : "xe khác cùng khung giờ";

            if (string.IsNullOrWhiteSpace(assignedBusLicensePlate))
                return $"Học sinh đi trễ và được điểm danh trên {actualRunType}";

            return $"Học sinh đi trễ và được điểm danh trên {actualRunType}, khác với xe được chia ban đầu ({assignedBusLicensePlate})";
        }

        private static string? NormalizeImageUrl(string? imageUrl)
        {
            return string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        }

        private static bool IsTimeWithinBusRun(TimeSpan candidateTime, TimeSpan startTime, TimeSpan? nextStartTime)
        {
            return candidateTime >= startTime &&
                   (!nextStartTime.HasValue || candidateTime < nextStartTime.Value);
        }

        private static bool IsEligibleAttendanceTransferWindow(TimeSpan bookingStartTime, TimeSpan actualBusRunStartTime)
        {
            if (actualBusRunStartTime < bookingStartTime)
                return false;

            return actualBusRunStartTime - bookingStartTime <= TimeSpan.FromMinutes(15);
        }

        private static AttendanceDto MapToDto(Attendance attendance)
        {
            return new AttendanceDto
            {
                Id = attendance.Id,
                StudentId = attendance.StudentId,
                StudentName = attendance.Student.FullName,
                GuardianId = attendance.Student.GuardianId,
                GuardianName = attendance.Student.Guardian?.FullName ?? string.Empty,
                BusId = attendance.BusId,
                BusLicensePlate = attendance.Bus.LicensePlate,
                Date = attendance.Date,
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                CheckInStationId = attendance.CheckInStationId,
                CheckInStationName = attendance.CheckInStation?.Name,
                CheckInImageUrl = attendance.CheckInImageUrl,
                CheckOutStationId = attendance.CheckOutStationId,
                CheckOutStationName = attendance.CheckOutStation?.Name,
                CheckOutImageUrl = attendance.CheckOutImageUrl,
                Note = attendance.Note,
                Method = attendance.Method.ToString(),
                Status = attendance.Status.ToString()
            };
        }

        private static AttendanceOnBusStudentDto MapToOnBusDto(Attendance attendance)
        {
            return new AttendanceOnBusStudentDto
            {
                AttendanceId = attendance.Id,
                StudentId = attendance.StudentId,
                StudentCode = attendance.Student.StudentCode,
                StudentName = attendance.Student.FullName,
                GuardianId = attendance.Student.GuardianId,
                GuardianName = attendance.Student.Guardian?.FullName ?? string.Empty,
                BusId = attendance.BusId,
                BusLicensePlate = attendance.Bus.LicensePlate,
                Date = attendance.Date,
                CheckInTime = attendance.CheckInTime ?? TimeSpan.Zero,
                CheckInStationId = attendance.CheckInStationId,
                CheckInStationName = attendance.CheckInStation?.Name,
                CheckInImageUrl = attendance.CheckInImageUrl,
                Note = attendance.Note,
                Method = attendance.Method.ToString(),
                Status = attendance.Status.ToString()
            };
        }

        private class ManualAttendanceValidationResult
        {
            public Student Student { get; set; } = null!;
            public Bus Bus { get; set; } = null!;
            public string RouteName { get; set; } = null!;
            public long? ExpectedDropOffStationId { get; set; }
            public string? ExpectedDropOffStationName { get; set; }
            public BusStation Station { get; set; } = null!;
            public DateTime AttendanceDate { get; set; }
            public TimeSpan CheckTime { get; set; }
            public long ActualBusRunId { get; set; }
            public BusRunStudent? SourceBusRunStudent { get; set; }
            public string? OperationalNote { get; set; }
        }
    }
}
