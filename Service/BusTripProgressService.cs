using BE_API.Common;
using BE_API.Dto.BusTripProgress;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusTripProgressService : IBusTripProgressService
    {
        private readonly IRepository<BusTripProgress> _progressRepo;
        private readonly IRepository<Bus> _busRepo;
        private readonly IRepository<BusAssignment> _busAssignmentRepo;
        private readonly IRepository<BusSchedule> _busScheduleRepo;
        private readonly IRepository<BusRouteStation> _routeStationRepo;
        private readonly IRepository<StudentBusAssignment> _studentBusAssignmentRepo;
        private readonly IRepository<Attendance> _attendanceRepo;
        private readonly IAppTime _appTime;

        public BusTripProgressService(
            IRepository<BusTripProgress> progressRepo,
            IRepository<Bus> busRepo,
            IRepository<BusAssignment> busAssignmentRepo,
            IRepository<BusSchedule> busScheduleRepo,
            IRepository<BusRouteStation> routeStationRepo,
            IRepository<StudentBusAssignment> studentBusAssignmentRepo,
            IRepository<Attendance> attendanceRepo,
            IAppTime appTime)
        {
            _progressRepo = progressRepo;
            _busRepo = busRepo;
            _busAssignmentRepo = busAssignmentRepo;
            _busScheduleRepo = busScheduleRepo;
            _routeStationRepo = routeStationRepo;
            _studentBusAssignmentRepo = studentBusAssignmentRepo;
            _attendanceRepo = attendanceRepo;
            _appTime = appTime;
        }

        public async Task<BusTripProgressEventDto> MarkArrivedAsync(BusTripProgressArriveDto dto)
        {
            var arrivedAtUtc = dto.ArrivedAt.HasValue
                ? _appTime.NormalizeToUtc(dto.ArrivedAt.Value)
                : _appTime.UtcNow;
            var rideCalendarDate = _appTime.GetCalendarDateForUtc(arrivedAtUtc);
            var schedule = await ValidateScheduleAsync(dto.BusId, dto.BusScheduleId, rideCalendarDate);
            var routeStations = await GetRouteStationsAsync(schedule.RouteId);
            var targetStation = routeStations
                .FirstOrDefault(x => x.StationId == dto.StationId)
                ?? throw new Exception("Trạm không thuộc tuyến của lịch chạy này");

            var progressList = await GetTripProgressListAsync(schedule.Id, rideCalendarDate);
            var latestProgress = progressList.LastOrDefault();
            var expectedStation = ResolveExpectedStation(routeStations, latestProgress);

            if (targetStation.OrderIndex != expectedStation.OrderIndex)
                throw new Exception($"Xe phải xác nhận đến trạm '{expectedStation.Station.Name}' trước");

            var progress = new BusTripProgress
            {
                BusId = schedule.BusId,
                BusScheduleId = schedule.Id,
                RouteId = schedule.RouteId,
                StationId = targetStation.StationId,
                RideDate = rideCalendarDate,
                OrderIndex = targetStation.OrderIndex,
                ArrivedAt = arrivedAtUtc
            };

            await _progressRepo.AddAsync(progress);
            await _progressRepo.SaveChangesAsync();

            var createdProgress = await GetProgressQueryable()
                .FirstOrDefaultAsync(x => x.Id == progress.Id)
                ?? throw new Exception("Không tìm thấy tiến trình chuyến xe");

            return MapEvent(createdProgress);
        }

        public async Task<List<BusTripProgressDriverScheduleDto>> GetDriverSchedulesAsync(long driverId, DateTime? rideDate, TimeSpan? atTime)
        {
            if (driverId <= 0)
                throw new Exception("DriverId phải lớn hơn 0");

            var assignment = await _busAssignmentRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Driver)
                .FirstOrDefaultAsync(x => x.DriverId == driverId)
                ?? throw new Exception("Tài xế chưa được phân công xe");

            return await BuildSchedulesForAssignmentAsync(
                assignment,
                rideDate,
                atTime,
                "Tài xế không có lịch chạy nào trong ngày đã chọn");
        }

        public async Task<List<BusTripProgressDriverScheduleDto>> GetTeacherSchedulesAsync(long teacherId, DateTime? rideDate, TimeSpan? atTime)
        {
            if (teacherId <= 0)
                throw new Exception("TeacherId phải lớn hơn 0");

            var assignment = await _busAssignmentRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.TeacherId == teacherId)
                ?? throw new Exception("Giáo viên chưa được phân công xe");

            return await BuildSchedulesForAssignmentAsync(
                assignment,
                rideDate,
                atTime,
                "Giáo viên không có lịch chạy nào trong ngày đã chọn");
        }

        private async Task<List<BusTripProgressDriverScheduleDto>> BuildSchedulesForAssignmentAsync(
            BusAssignment assignment,
            DateTime? rideDate,
            TimeSpan? atTime,
            string emptyMessage)
        {
            var selectedDate = _appTime.GetRideCalendarDate(rideDate);
            var selectedTime = atTime ?? _appTime.GetTimeOfDay();
            var dayOfWeek = ScheduleDayOfWeek.FromDate(selectedDate);

            var schedules = await _busScheduleRepo.Get()
                .Include(x => x.Route)
                .Where(x =>
                    x.BusId == assignment.BusId &&
                    x.IsActive &&
                    x.StartDate.Date <= selectedDate &&
                    (!x.EndDate.HasValue || x.EndDate.Value.Date >= selectedDate) &&
                    x.DayOfWeek == dayOfWeek)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.EndTime)
                .ToListAsync();

            if (!schedules.Any())
                throw new Exception(emptyMessage);

            var runningSchedules = schedules
                .Where(x => x.StartTime <= selectedTime && x.EndTime >= selectedTime)
                .ToList();

            var recommendedScheduleId = runningSchedules.Count == 1
                ? runningSchedules[0].Id
                : schedules.FirstOrDefault(x => x.StartTime > selectedTime)?.Id
                    ?? schedules.Last().Id;

            var scheduleIds = schedules.Select(x => x.Id).ToList();
            var routeIds = schedules.Select(x => x.RouteId).Distinct().ToList();

            var routeStations = await _routeStationRepo.Get()
                .Include(x => x.Station)
                .Where(x => routeIds.Contains(x.RouteId))
                .OrderBy(x => x.RouteId)
                .ThenBy(x => x.OrderIndex)
                .ToListAsync();

            var progressBySchedule = await GetProgressQueryable()
                .Where(x => scheduleIds.Contains(x.BusScheduleId) && x.RideDate.Date == selectedDate)
                .OrderBy(x => x.OrderIndex)
                .ThenBy(x => x.ArrivedAt)
                .ToListAsync();

            return schedules.Select(x =>
            {
                var busLabel = !string.IsNullOrWhiteSpace(assignment.Bus.BusNumber)
                    ? assignment.Bus.BusNumber
                    : assignment.Bus.LicensePlate;

                var stations = routeStations
                    .Where(s => s.RouteId == x.RouteId)
                    .Select(routeStation =>
                    {
                        var progress = progressBySchedule
                            .FirstOrDefault(p =>
                                p.BusScheduleId == x.Id &&
                                p.OrderIndex == routeStation.OrderIndex);

                        return new BusTripProgressStationStatusDto
                        {
                            StationId = routeStation.StationId,
                            StationName = routeStation.Station.Name,
                            Latitude = routeStation.Station.Latitude,
                            Longitude = routeStation.Station.Longitude,
                            OrderIndex = routeStation.OrderIndex,
                            IsVisited = progress != null,
                            ArrivedAt = progress?.ArrivedAt
                        };
                    })
                    .ToList();

                return new BusTripProgressDriverScheduleDto
                {
                    BusScheduleId = x.Id,
                    BusId = x.BusId,
                    BusLabel = busLabel,
                    RouteId = x.RouteId,
                    RouteName = x.Route.Name,
                    RideDate = selectedDate,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    ShiftType = x.ShiftType,
                    IsRunningNow = x.StartTime <= selectedTime && x.EndTime >= selectedTime,
                    IsUpcoming = selectedTime < x.StartTime,
                    IsCompleted = selectedTime > x.EndTime,
                    IsRecommended = x.Id == recommendedScheduleId,
                    Stations = stations
                };
            }).ToList();
        }

        public async Task<BusTripProgressCurrentDto> GetCurrentAsync(long busId, long busScheduleId, DateTime? rideDate)
        {
            var selectedDate = _appTime.GetRideCalendarDate(rideDate);
            var schedule = await ValidateScheduleAsync(busId, busScheduleId, selectedDate);
            var routeStations = await GetRouteStationsAsync(schedule.RouteId);
            var progressList = await GetTripProgressListAsync(schedule.Id, selectedDate);
            var latestProgress = progressList.LastOrDefault();

            var nextStation = latestProgress == null
                ? routeStations.FirstOrDefault()
                : routeStations.FirstOrDefault(x => x.OrderIndex > latestProgress.OrderIndex);

            var tripStatus = ResolveTripStatus(latestProgress, nextStation);

            return new BusTripProgressCurrentDto
            {
                BusId = schedule.BusId,
                BusScheduleId = schedule.Id,
                RouteId = schedule.RouteId,
                RouteName = schedule.Route.Name,
                RideDate = selectedDate,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                TripStatus = tripStatus,
                CurrentStationId = latestProgress?.StationId,
                CurrentStationName = latestProgress?.Station.Name,
                ArrivedAt = latestProgress?.ArrivedAt,
                NextStationId = nextStation?.StationId,
                NextStationName = nextStation?.Station.Name,
                NextOrderIndex = nextStation?.OrderIndex,
                IsCompleted = tripStatus == "COMPLETED",
                Stations = routeStations
                    .Select(routeStation =>
                    {
                        var progress = progressList.FirstOrDefault(x => x.OrderIndex == routeStation.OrderIndex);
                        return new BusTripProgressStationStatusDto
                        {
                            StationId = routeStation.StationId,
                            StationName = routeStation.Station.Name,
                            Latitude = routeStation.Station.Latitude,
                            Longitude = routeStation.Station.Longitude,
                            OrderIndex = routeStation.OrderIndex,
                            IsVisited = progress != null,
                            ArrivedAt = progress?.ArrivedAt
                        };
                    })
                    .ToList()
            };
        }

        public async Task<List<BusTripProgressHistoryDto>> GetHistoryAsync(long? busId, long? routeId, long? campusId, DateTime? fromDate, DateTime? toDate)
        {
            if (busId.HasValue && busId.Value <= 0)
                throw new Exception("BusId phải lớn hơn 0");

            if (routeId.HasValue && routeId.Value <= 0)
                throw new Exception("RouteId phải lớn hơn 0");

            if (campusId.HasValue && campusId.Value <= 0)
                throw new Exception("CampusId phải lớn hơn 0");

            var today = _appTime.TodayDate;
            var to = _appTime.GetRideCalendarDate(toDate);
            if (to > today)
                to = today;

            var from = fromDate.HasValue
                ? _appTime.GetRideCalendarDate(fromDate)
                : to.AddDays(-7);

            if (from > to)
                throw new Exception("Từ ngày phải nhỏ hơn hoặc bằng đến ngày");

            var schedules = await _busScheduleRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Route)
                .ThenInclude(x => x.Campus)
                .Where(x =>
                    x.StartDate.Date <= to &&
                    (!x.EndDate.HasValue || x.EndDate.Value.Date >= from))
                .Where(x => !busId.HasValue || x.BusId == busId.Value)
                .Where(x => !routeId.HasValue || x.RouteId == routeId.Value)
                .Where(x => !campusId.HasValue || x.Route.CampusId == campusId.Value)
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.StartTime)
                .ToListAsync();

            if (!schedules.Any())
                return new List<BusTripProgressHistoryDto>();

            var scheduleIds = schedules.Select(x => x.Id).ToList();
            var busIds = schedules.Select(x => x.BusId).Distinct().ToList();
            var routeIds = schedules.Select(x => x.RouteId).Distinct().ToList();

            var assignments = await _busAssignmentRepo.Get()
                .Include(x => x.Driver)
                .Include(x => x.Teacher)
                .Where(x => busIds.Contains(x.BusId))
                .ToListAsync();

            var routeStationCounts = await _routeStationRepo.Get()
                .Where(x => routeIds.Contains(x.RouteId))
                .GroupBy(x => x.RouteId)
                .Select(x => new { RouteId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.RouteId, x => x.Count);

            var progressList = await GetProgressQueryable()
                .Where(x =>
                    scheduleIds.Contains(x.BusScheduleId) &&
                    x.RideDate.Date >= from &&
                    x.RideDate.Date <= to)
                .OrderBy(x => x.RideDate)
                .ThenBy(x => x.OrderIndex)
                .ThenBy(x => x.ArrivedAt)
                .ToListAsync();

            var studentAssignments = await _studentBusAssignmentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.PickupStation)
                .Include(x => x.DropOffStation)
                .Where(x =>
                    routeIds.Contains(x.RouteId) &&
                    (!x.RideDate.HasValue || (x.RideDate.Value.Date >= from && x.RideDate.Value.Date <= to)))
                .ToListAsync();

            var attendances = await _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.CheckInStation)
                .Include(x => x.CheckOutStation)
                .Where(x =>
                    busIds.Contains(x.BusId) &&
                    x.Date.Date >= from &&
                    x.Date.Date <= to)
                .ToListAsync();

            var results = new List<BusTripProgressHistoryDto>();

            foreach (var schedule in schedules)
            {
                var assignment = assignments.FirstOrDefault(x => x.BusId == schedule.BusId);
                var totalStationCount = routeStationCounts.TryGetValue(schedule.RouteId, out var stationCount)
                    ? stationCount
                    : 0;

                foreach (var rideDateValue in EnumerateRideDates(schedule, from, to))
                {
                    var rideDateOnly = rideDateValue.Date;

                    var tripProgress = progressList
                        .Where(x => x.BusScheduleId == schedule.Id && x.RideDate.Date == rideDateOnly)
                        .ToList();

                    var tripAttendances = attendances
                        .Where(x => x.BusId == schedule.BusId && x.Date.Date == rideDateOnly)
                        .Where(x => AttendanceMatchesSchedule(x, schedule))
                        .ToList();

                    var plannedStudentCount = studentAssignments
                        .Count(x =>
                            x.RouteId == schedule.RouteId &&
                            (!x.RideDate.HasValue || x.RideDate.Value.Date == rideDateOnly));

                    var assignedStudents = studentAssignments
                        .Where(x =>
                            x.RouteId == schedule.RouteId &&
                            (!x.RideDate.HasValue || x.RideDate.Value.Date == rideDateOnly))
                        .GroupBy(x => x.StudentId)
                        .Select(x => x.OrderByDescending(y => y.RideDate.HasValue).First())
                        .ToList();

                    var actualStudentCount = tripAttendances
                        .Select(x => x.StudentId)
                        .Distinct()
                        .Count();

                    var students = assignedStudents
                        .Select(x =>
                        {
                            return new BusTripProgressHistoryStudentDto
                            {
                                StudentId = x.StudentId,
                                StudentCode = x.Student.StudentCode,
                                StudentName = x.Student.FullName,
                                PickupStationId = x.PickupStationId,
                                PickupStationName = x.PickupStation?.Name,
                                DropOffStationId = x.DropOffStationId,
                                DropOffStationName = x.DropOffStation?.Name,
                                AssignmentType = x.RideDate.HasValue ? "ONE_TIME" : "RECURRING"
                            };
                        })
                        .OrderBy(x => x.StudentCode)
                        .ThenBy(x => x.StudentName)
                        .ToList();

                    var actualTimeline = ResolveActualTimeline(rideDateOnly, schedule, tripProgress, tripAttendances);
                    var visitedStationCount = tripProgress
                        .Select(x => x.OrderIndex)
                        .Distinct()
                        .Count();
                    var isCompleted = totalStationCount > 0 && visitedStationCount >= totalStationCount;

                    results.Add(new BusTripProgressHistoryDto
                    {
                        BusScheduleId = schedule.Id,
                        BusId = schedule.BusId,
                        BusLabel = !string.IsNullOrWhiteSpace(schedule.Bus.BusNumber)
                            ? schedule.Bus.BusNumber
                            : schedule.Bus.LicensePlate,
                        RouteId = schedule.RouteId,
                        RouteName = schedule.Route.Name,
                        CampusId = schedule.Route.CampusId,
                        CampusName = schedule.Route.Campus.Name,
                        RideDate = rideDateOnly,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        ShiftType = schedule.ShiftType,
                        DriverId = assignment?.DriverId,
                        DriverName = assignment?.Driver?.FullName,
                        TeacherId = assignment?.TeacherId,
                        TeacherName = assignment?.Teacher?.FullName,
                        PlannedStudentCount = plannedStudentCount,
                        ActualStudentCount = actualStudentCount,
                        VisitedStationCount = visitedStationCount,
                        TotalStationCount = totalStationCount,
                        ActualStartAt = actualTimeline.ActualStartAt,
                        ActualEndAt = actualTimeline.ActualEndAt,
                        IsCompleted = isCompleted,
                        TripStatus = ResolveHistoryTripStatus(isCompleted, visitedStationCount, actualStudentCount),
                        Students = students
                    });
                }
            }

            return results
                .OrderByDescending(x => x.RideDate)
                .ThenByDescending(x => x.StartTime)
                .ToList();
        }

        private IQueryable<BusTripProgress> GetProgressQueryable()
        {
            return _progressRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.Station)
                .Include(x => x.Bus)
                .Include(x => x.BusSchedule);
        }

        private async Task<Bus> ValidateBusAsync(long busId)
        {
            if (busId <= 0)
                throw new Exception("BusId phải lớn hơn 0");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == busId)
                ?? throw new Exception("Bus không tồn tại");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bus đang không hoạt động");

            return bus;
        }

        private async Task<BusSchedule> ValidateScheduleAsync(long busId, long busScheduleId, DateTime rideDate)
        {
            await ValidateBusAsync(busId);

            if (busScheduleId <= 0)
                throw new Exception("BusScheduleId phải lớn hơn 0");

            var scheduleById = await _busScheduleRepo.Get()
                .Include(x => x.Route)
                .FirstOrDefaultAsync(x => x.Id == busScheduleId);

            if (scheduleById == null)
                throw new Exception($"Không tìm thấy lịch chạy với BusScheduleId = {busScheduleId}");

            if (scheduleById.BusId != busId)
                throw new Exception(
                    $"BusScheduleId = {busScheduleId} thuộc xe {scheduleById.BusId}, không phải xe {busId}");

            if (!scheduleById.IsActive)
                throw new Exception($"BusScheduleId = {busScheduleId} đang ở trạng thái không hoạt động");

            var dayOfWeek = ScheduleDayOfWeek.FromDate(rideDate);

            if (scheduleById.StartDate.Date > rideDate.Date)
                throw new Exception(
                    $"Lịch chạy {busScheduleId} chỉ bắt đầu từ ngày {scheduleById.StartDate:yyyy-MM-dd}, chưa áp dụng cho ngày {rideDate:yyyy-MM-dd}");

            if (scheduleById.EndDate.HasValue && scheduleById.EndDate.Value.Date < rideDate.Date)
                throw new Exception(
                    $"Lịch chạy {busScheduleId} đã hết hạn vào ngày {scheduleById.EndDate.Value:yyyy-MM-dd}");

            if (scheduleById.DayOfWeek != dayOfWeek)
                throw new Exception(
                    $"Lịch chạy {busScheduleId} áp dụng cho DayOfWeek = {scheduleById.DayOfWeek}, không khớp ngày {rideDate:yyyy-MM-dd} (DayOfWeek = {dayOfWeek})");

            return scheduleById;
        }

        private async Task<List<BusRouteStation>> GetRouteStationsAsync(long routeId)
        {
            var routeStations = await _routeStationRepo.Get()
                .Include(x => x.Station)
                .Where(x => x.RouteId == routeId)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();

            if (!routeStations.Any())
                throw new Exception("Tuyến xe chưa có danh sách trạm");

            var disabledStation = routeStations.FirstOrDefault(x => !x.Station.IsEnabled);
            if (disabledStation != null)
                throw new Exception($"Trạm '{disabledStation.Station.Name}' đang không hoạt động");

            return routeStations;
        }

        private async Task<List<BusTripProgress>> GetTripProgressListAsync(long busScheduleId, DateTime rideDate)
        {
            return await GetProgressQueryable()
                .Where(x => x.BusScheduleId == busScheduleId && x.RideDate.Date == rideDate.Date)
                .OrderBy(x => x.OrderIndex)
                .ThenBy(x => x.ArrivedAt)
                .ToListAsync();
        }

        private static BusRouteStation ResolveExpectedStation(List<BusRouteStation> routeStations, BusTripProgress? latestProgress)
        {
            if (latestProgress == null)
                return routeStations[0];

            var expectedStation = routeStations
                .FirstOrDefault(x => x.OrderIndex == latestProgress.OrderIndex + 1);

            if (expectedStation == null)
                throw new Exception("Xe đã xác nhận hết tất cả các trạm của tuyến");

            return expectedStation;
        }

        private static string ResolveTripStatus(BusTripProgress? latestProgress, BusRouteStation? nextStation)
        {
            if (latestProgress == null)
                return "NOT_STARTED";

            if (nextStation == null)
                return "COMPLETED";

            return "AT_STATION";
        }

        private IEnumerable<DateTime> EnumerateRideDates(BusSchedule schedule, DateTime from, DateTime to)
        {
            var effectiveStart = schedule.StartDate.Date > from ? schedule.StartDate.Date : from;
            var scheduleEndDate = schedule.EndDate?.Date ?? to;
            var effectiveEnd = scheduleEndDate < to ? scheduleEndDate : to;

            for (var date = effectiveStart.Date; date <= effectiveEnd.Date; date = date.AddDays(1))
            {
                if (ScheduleDayOfWeek.FromDate(date) == schedule.DayOfWeek)
                    yield return date;
            }
        }

        private static bool AttendanceMatchesSchedule(Attendance attendance, BusSchedule schedule)
        {
            return schedule.ShiftType switch
            {
                "PICKUP" => attendance.CheckInTime.HasValue && attendance.CheckInTime.Value >= schedule.StartTime && attendance.CheckInTime.Value <= schedule.EndTime,
                "DROPOFF" => attendance.CheckOutTime.HasValue && attendance.CheckOutTime.Value >= schedule.StartTime && attendance.CheckOutTime.Value <= schedule.EndTime,
                "ROUNDTRIP" =>
                    (attendance.CheckInTime.HasValue && attendance.CheckInTime.Value >= schedule.StartTime && attendance.CheckInTime.Value <= schedule.EndTime) ||
                    (attendance.CheckOutTime.HasValue && attendance.CheckOutTime.Value >= schedule.StartTime && attendance.CheckOutTime.Value <= schedule.EndTime),
                _ => false
            };
        }

        private static (DateTime? ActualStartAt, DateTime? ActualEndAt) ResolveActualTimeline(
            DateTime rideDate,
            BusSchedule schedule,
            List<BusTripProgress> tripProgress,
            List<Attendance> tripAttendances)
        {
            if (tripProgress.Any())
                return (tripProgress.Min(x => x.ArrivedAt), tripProgress.Max(x => x.ArrivedAt));

            var actualTimes = tripAttendances
                .SelectMany(x => ResolveAttendanceTimes(rideDate, schedule, x))
                .OrderBy(x => x)
                .ToList();

            if (!actualTimes.Any())
                return (null, null);

            return (actualTimes.First(), actualTimes.Last());
        }

        private static IEnumerable<DateTime> ResolveAttendanceTimes(DateTime rideDate, BusSchedule schedule, Attendance attendance)
        {
            if ((schedule.ShiftType == "PICKUP" || schedule.ShiftType == "ROUNDTRIP") &&
                attendance.CheckInTime.HasValue &&
                attendance.CheckInTime.Value >= schedule.StartTime &&
                attendance.CheckInTime.Value <= schedule.EndTime)
            {
                yield return rideDate.Date.Add(attendance.CheckInTime.Value);
            }

            if ((schedule.ShiftType == "DROPOFF" || schedule.ShiftType == "ROUNDTRIP") &&
                attendance.CheckOutTime.HasValue &&
                attendance.CheckOutTime.Value >= schedule.StartTime &&
                attendance.CheckOutTime.Value <= schedule.EndTime)
            {
                yield return rideDate.Date.Add(attendance.CheckOutTime.Value);
            }
        }

        private static string ResolveHistoryTripStatus(bool isCompleted, int visitedStationCount, int actualStudentCount)
        {
            if (isCompleted)
                return "COMPLETED";

            if (visitedStationCount > 0)
                return "IN_PROGRESS";

            if (actualStudentCount > 0)
                return "HAS_ATTENDANCE";

            return "NO_DATA";
        }

        private static BusTripProgressEventDto MapEvent(BusTripProgress progress)
        {
            return new BusTripProgressEventDto
            {
                Id = progress.Id,
                BusId = progress.BusId,
                BusScheduleId = progress.BusScheduleId,
                RouteId = progress.RouteId,
                RouteName = progress.Route.Name,
                StationId = progress.StationId,
                StationName = progress.Station.Name,
                OrderIndex = progress.OrderIndex,
                RideDate = progress.RideDate,
                ArrivedAt = progress.ArrivedAt
            };
        }
    }
}
