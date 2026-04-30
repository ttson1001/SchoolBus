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
        private readonly IRepository<BusRun> _busRunRepo;
        private readonly IRepository<BusRunStudent> _busRunStudentRepo;
        private readonly IRepository<BusRouteStation> _routeStationRepo;
        private readonly IRepository<Attendance> _attendanceRepo;
        private readonly IAppTime _appTime;

        public BusTripProgressService(
            IRepository<BusTripProgress> progressRepo,
            IRepository<Bus> busRepo,
            IRepository<BusRun> busRunRepo,
            IRepository<BusRunStudent> busRunStudentRepo,
            IRepository<BusRouteStation> routeStationRepo,
            IRepository<Attendance> attendanceRepo,
            IAppTime appTime)
        {
            _progressRepo = progressRepo;
            _busRepo = busRepo;
            _busRunRepo = busRunRepo;
            _busRunStudentRepo = busRunStudentRepo;
            _routeStationRepo = routeStationRepo;
            _attendanceRepo = attendanceRepo;
            _appTime = appTime;
        }

        public async Task<BusTripProgressEventDto> MarkArrivedAsync(BusTripProgressArriveDto dto)
        {
            var arrivedAtUtc = dto.ArrivedAt.HasValue
                ? _appTime.NormalizeToUtc(dto.ArrivedAt.Value)
                : _appTime.UtcNow;
            var rideDate = _appTime.GetCalendarDateForUtc(arrivedAtUtc);

            var busRun = await ValidateBusRunAsync(dto.BusId, dto.BusRunId, rideDate);
            var routeStations = await GetRouteStationsAsync(busRun.RouteId);
            var targetStation = routeStations.FirstOrDefault(x => x.StationId == dto.StationId)
                ?? throw new Exception("Tram khong thuoc tuyen cua chuyen xe nay");

            var progressList = await GetTripProgressListAsync(busRun.Id, rideDate);
            var expectedStation = ResolveExpectedStation(routeStations, progressList.LastOrDefault());

            if (targetStation.OrderIndex != expectedStation.OrderIndex)
                throw new Exception($"Xe phai xac nhan den tram '{expectedStation.Station.Name}' truoc");

            var progress = new BusTripProgress
            {
                BusId = busRun.BusId,
                BusRunId = busRun.Id,
                RouteId = busRun.RouteId,
                StationId = targetStation.StationId,
                RideDate = rideDate,
                OrderIndex = targetStation.OrderIndex,
                ArrivedAt = arrivedAtUtc
            };

            await _progressRepo.AddAsync(progress);
            await _progressRepo.SaveChangesAsync();

            var createdProgress = await GetProgressQueryable()
                .FirstOrDefaultAsync(x => x.Id == progress.Id)
                ?? throw new Exception("Khong tim thay tien trinh chuyen xe");

            return MapEvent(createdProgress);
        }

        public async Task<List<BusTripProgressDriverScheduleDto>> GetDriverSchedulesAsync(long driverId, DateTime? rideDate, TimeSpan? atTime)
        {
            if (driverId <= 0)
                throw new Exception("DriverId phai lon hon 0");

            var selectedDate = _appTime.GetRideCalendarDate(rideDate);
            var runs = await GetBusRunQueryable()
                .Where(x => x.DriverId == driverId && x.ServiceDate.Date == selectedDate)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.RunOrder)
                .ToListAsync();

            return await BuildSchedulesForRunsAsync(
                runs,
                selectedDate,
                atTime,
                "Tai xe khong co lich chay nao trong ngay da chon");
        }

        public async Task<List<BusTripProgressDriverScheduleDto>> GetTeacherSchedulesAsync(long teacherId, DateTime? rideDate, TimeSpan? atTime)
        {
            if (teacherId <= 0)
                throw new Exception("TeacherId phai lon hon 0");

            var selectedDate = _appTime.GetRideCalendarDate(rideDate);
            var runs = await GetBusRunQueryable()
                .Where(x => x.TeacherId == teacherId && x.ServiceDate.Date == selectedDate)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.RunOrder)
                .ToListAsync();

            return await BuildSchedulesForRunsAsync(
                runs,
                selectedDate,
                atTime,
                "Giao vien khong co lich chay nao trong ngay da chon");
        }

        public async Task<BusTripProgressCurrentDto> GetCurrentAsync(long busId, long busRunId, DateTime? rideDate)
        {
            var selectedDate = _appTime.GetRideCalendarDate(rideDate);
            var busRun = await ValidateBusRunAsync(busId, busRunId, selectedDate);
            var routeStations = await GetRouteStationsAsync(busRun.RouteId);
            var progressList = await GetTripProgressListAsync(busRun.Id, selectedDate);
            var latestProgress = progressList.LastOrDefault();
            var runStudentAssignments = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Include(x => x.BusRun)
                .Where(x =>
                    x.BusRun.ServiceDate.Date == selectedDate &&
                    x.BusRun.RouteId == busRun.RouteId &&
                    x.BusRun.StartTime == busRun.StartTime)
                .ToListAsync();

            var studentIds = runStudentAssignments
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var attendances = await _attendanceRepo.Get()
                .Include(x => x.Bus)
                .Where(x =>
                    studentIds.Contains(x.StudentId) &&
                    x.Date.Date == selectedDate)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var displayStudents = BuildDisplayStudentsForRun(
                busRun,
                selectedDate,
                runStudentAssignments,
                attendances);

            var nextStation = latestProgress == null
                ? routeStations.FirstOrDefault()
                : routeStations.FirstOrDefault(x => x.OrderIndex > latestProgress.OrderIndex);

            return new BusTripProgressCurrentDto
            {
                BusId = busRun.BusId,
                BusRunId = busRun.Id,
                RouteId = busRun.RouteId,
                RouteName = busRun.Route.Name,
                RideDate = selectedDate,
                StartTime = busRun.StartTime,
                TripStatus = ResolveTripStatus(latestProgress, nextStation),
                CurrentStationId = latestProgress?.StationId,
                CurrentStationName = latestProgress?.Station.Name,
                ArrivedAt = latestProgress?.ArrivedAt,
                NextStationId = nextStation?.StationId,
                NextStationName = nextStation?.Station.Name,
                NextOrderIndex = nextStation?.OrderIndex,
                IsCompleted = nextStation == null && latestProgress != null,
                Stations = routeStations.Select(routeStation =>
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
                        ArrivedAt = progress?.ArrivedAt,
                        Students = displayStudents
                            .Where(x => x.Booking.StationId == routeStation.StationId)
                            .Select(x =>
                            {
                                var checkedInOnThisBus = HasCheckedInOnThisBus(attendances, busRun, x.StudentId);
                                var currentlyOnThisBus = IsCurrentlyOnThisBus(attendances, busRun, x.StudentId);
                                var activeOnOtherBus = GetActiveAttendanceOnOtherBus(attendances, selectedDate, busRun, x.StudentId);

                                return new BusTripProgressStationStudentDto
                                {
                                    StudentId = x.StudentId,
                                    StudentCode = x.Student.StudentCode,
                                    StudentName = x.Student.FullName,
                                    StationId = x.Booking.StationId,
                                    StationName = x.Booking.Station?.Name ?? string.Empty,
                                    PickupAddress = x.Booking.PickupAddress,
                                    PickupLatitude = x.Booking.Latitude,
                                    PickupLongitude = x.Booking.Longitude,
                                    HasCheckedInOnThisBus = checkedInOnThisBus,
                                    IsCurrentlyOnThisBus = currentlyOnThisBus,
                                    CurrentBusId = activeOnOtherBus?.BusId,
                                    CurrentBusLabel = activeOnOtherBus?.Bus != null
                                        ? ResolveBusLabel(activeOnOtherBus.Bus)
                                        : null,
                                    IsOnDifferentBusThanAssigned = activeOnOtherBus != null
                                };
                            })
                            .OrderBy(x => x.PickupLatitude ?? double.MaxValue)
                            .ThenBy(x => x.PickupLongitude ?? double.MaxValue)
                            .ThenBy(x => x.StudentCode)
                            .ThenBy(x => x.StudentName)
                            .ToList()
                    };
                }).ToList()
            };
        }

        public async Task<List<BusTripProgressHistoryDto>> GetHistoryAsync(long? busId, long? routeId, long? campusId, DateTime? fromDate, DateTime? toDate)
        {
            if (busId.HasValue && busId.Value <= 0)
                throw new Exception("BusId phai lon hon 0");

            if (routeId.HasValue && routeId.Value <= 0)
                throw new Exception("RouteId phai lon hon 0");

            if (campusId.HasValue && campusId.Value <= 0)
                throw new Exception("CampusId phai lon hon 0");

            var today = _appTime.TodayDate;
            var to = _appTime.GetRideCalendarDate(toDate);
            if (to > today)
                to = today;

            var from = fromDate.HasValue
                ? _appTime.GetRideCalendarDate(fromDate)
                : to.AddDays(-7);

            if (from > to)
                throw new Exception("Tu ngay phai nho hon hoac bang den ngay");

            var busRuns = await GetBusRunQueryable()
                .Where(x => x.ServiceDate.Date >= from && x.ServiceDate.Date <= to)
                .Where(x => !busId.HasValue || x.BusId == busId.Value)
                .Where(x => !routeId.HasValue || x.RouteId == routeId.Value)
                .Where(x => !campusId.HasValue || x.Route.CampusId == campusId.Value)
                .OrderByDescending(x => x.ServiceDate)
                .ThenByDescending(x => x.StartTime)
                .ThenBy(x => x.RunOrder)
                .ToListAsync();

            if (!busRuns.Any())
                return new List<BusTripProgressHistoryDto>();

            var runIds = busRuns.Select(x => x.Id).ToList();
            var routeIds = busRuns.Select(x => x.RouteId).Distinct().ToList();
            var busIds = busRuns.Select(x => x.BusId).Distinct().ToList();
            var startTimes = busRuns.Select(x => x.StartTime).Distinct().ToList();

            var routeStationCounts = await _routeStationRepo.Get()
                .Where(x => routeIds.Contains(x.RouteId))
                .GroupBy(x => x.RouteId)
                .Select(x => new { RouteId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.RouteId, x => x.Count);

            var progressList = await GetProgressQueryable()
                .Where(x => runIds.Contains(x.BusRunId))
                .OrderBy(x => x.RideDate)
                .ThenBy(x => x.OrderIndex)
                .ThenBy(x => x.ArrivedAt)
                .ToListAsync();

            var runStudentAssignments = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Include(x => x.BusRun)
                .Where(x =>
                    x.BusRun.ServiceDate.Date >= from &&
                    x.BusRun.ServiceDate.Date <= to &&
                    routeIds.Contains(x.BusRun.RouteId) &&
                    startTimes.Contains(x.BusRun.StartTime))
                .ToListAsync();

            var attendances = await _attendanceRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Student)
                .Where(x => busIds.Contains(x.BusId) && x.Date.Date >= from && x.Date.Date <= to)
                .ToListAsync();

            var results = new List<BusTripProgressHistoryDto>();

            foreach (var run in busRuns)
            {
                var rideDateOnly = run.ServiceDate.Date;
                var tripProgress = progressList
                    .Where(x => x.BusRunId == run.Id && x.RideDate.Date == rideDateOnly)
                    .ToList();

                var sameDayRuns = busRuns
                    .Where(x => x.BusId == run.BusId && x.ServiceDate.Date == rideDateOnly)
                    .ToList();

                var tripAttendances = attendances
                    .Where(x => x.BusId == run.BusId && x.Date.Date == rideDateOnly)
                    .Where(x => AttendanceMatchesRun(x, run, sameDayRuns))
                    .ToList();

                var displayStudents = BuildDisplayStudentsForRun(
                    run,
                    rideDateOnly,
                    runStudentAssignments,
                    tripAttendances);

                var totalStationCount = routeStationCounts.TryGetValue(run.RouteId, out var stationCount)
                    ? stationCount
                    : 0;

                var actualTimeline = ResolveActualTimeline(rideDateOnly, run, tripProgress, tripAttendances, sameDayRuns);
                var visitedStationCount = tripProgress.Select(x => x.OrderIndex).Distinct().Count();
                var actualStudentCount = tripAttendances.Select(x => x.StudentId).Distinct().Count();
                var isCompleted = totalStationCount > 0 && visitedStationCount >= totalStationCount;

                results.Add(new BusTripProgressHistoryDto
                {
                    BusRunId = run.Id,
                    BusId = run.BusId,
                    BusLabel = ResolveBusLabel(run.Bus),
                    RouteId = run.RouteId,
                    RouteName = run.Route.Name,
                    CampusId = run.Route.CampusId,
                    CampusName = run.Route.Campus.Name,
                    RideDate = rideDateOnly,
                    StartTime = run.StartTime,
                    ShiftType = run.Status,
                    DriverId = run.DriverId,
                    DriverName = run.Driver?.FullName ?? run.Driver?.Email,
                    TeacherId = run.TeacherId,
                    TeacherName = run.Teacher?.FullName ?? run.Teacher?.Email,
                    PlannedStudentCount = runStudentAssignments
                        .Where(x => x.BusRunId == run.Id)
                        .Select(x => x.StudentId)
                        .Distinct()
                        .Count(),
                    ActualStudentCount = actualStudentCount,
                    VisitedStationCount = visitedStationCount,
                    TotalStationCount = totalStationCount,
                    ActualStartAt = actualTimeline.ActualStartAt,
                    ActualEndAt = actualTimeline.ActualEndAt,
                    IsCompleted = isCompleted,
                    TripStatus = ResolveHistoryTripStatus(isCompleted, visitedStationCount, actualStudentCount),
                    Students = displayStudents
                        .Select(x =>
                        {
                            var checkedInOnThisBus = HasCheckedInOnThisBus(tripAttendances, run, x.StudentId);
                            var currentlyOnThisBus = IsCurrentlyOnThisBus(tripAttendances, run, x.StudentId);
                            var activeOnOtherBus = GetActiveAttendanceOnOtherBus(attendances, rideDateOnly, run, x.StudentId);

                            var currentBusLabel = activeOnOtherBus?.Bus != null
                                ? ResolveBusLabel(activeOnOtherBus.Bus)
                                : null;

                            return new BusTripProgressHistoryStudentDto
                            {
                                StudentId = x.StudentId,
                                StudentCode = x.Student.StudentCode,
                                StudentName = x.Student.FullName,
                                StationId = x.Booking.StationId,
                                StationName = x.Booking.Station?.Name,
                                PickupAddress = x.Booking.PickupAddress,
                                AssignmentType = "BOOKING",
                                HasCheckedInOnThisBus = checkedInOnThisBus,
                                IsCurrentlyOnThisBus = currentlyOnThisBus,
                                CurrentBusId = activeOnOtherBus?.BusId,
                                CurrentBusLabel = currentBusLabel,
                                IsOnDifferentBusThanAssigned = activeOnOtherBus != null
                            };
                        })
                        .OrderBy(x => x.StudentCode)
                        .ThenBy(x => x.StudentName)
                        .ToList()
                });
            }

            return results
                .OrderByDescending(x => x.RideDate)
                .ThenByDescending(x => x.StartTime)
                .ToList();
        }

        private async Task<List<BusTripProgressDriverScheduleDto>> BuildSchedulesForRunsAsync(
            List<BusRun> runs,
            DateTime? rideDate,
            TimeSpan? atTime,
            string emptyMessage)
        {
            if (!runs.Any())
                throw new Exception(emptyMessage);

            var selectedDate = _appTime.GetRideCalendarDate(rideDate);
            var selectedTime = atTime ?? _appTime.GetTimeOfDay();
            var runIds = runs.Select(x => x.Id).ToList();
            var routeIds = runs.Select(x => x.RouteId).Distinct().ToList();
            var startTimes = runs.Select(x => x.StartTime).Distinct().ToList();

            var routeStations = await _routeStationRepo.Get()
                .Include(x => x.Station)
                .Where(x => routeIds.Contains(x.RouteId))
                .OrderBy(x => x.RouteId)
                .ThenBy(x => x.OrderIndex)
                .ToListAsync();

            var progressByRun = await GetProgressQueryable()
                .Where(x => runIds.Contains(x.BusRunId) && x.RideDate.Date == selectedDate)
                .OrderBy(x => x.OrderIndex)
                .ThenBy(x => x.ArrivedAt)
                .ToListAsync();

            var runStudentAssignments = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Include(x => x.BusRun)
                .Where(x =>
                    x.BusRun.ServiceDate.Date == selectedDate &&
                    routeIds.Contains(x.BusRun.RouteId) &&
                    startTimes.Contains(x.BusRun.StartTime))
                .ToListAsync();

            var studentIds = runStudentAssignments
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var attendances = await _attendanceRepo.Get()
                .Include(x => x.Bus)
                .Where(x =>
                    studentIds.Contains(x.StudentId) &&
                    x.Date.Date == selectedDate)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var runningRuns = runs
                .Where(x => IsRunActiveNow(x, runs, selectedTime))
                .ToList();

            var recommendedRunId = runningRuns.Count == 1
                ? runningRuns[0].Id
                : runs.FirstOrDefault(x => x.StartTime > selectedTime)?.Id
                    ?? runs.Last().Id;

            return runs.Select(x =>
            {
                var stations = routeStations
                    .Where(s => s.RouteId == x.RouteId)
                    .Select(routeStation =>
                    {
                        var progress = progressByRun.FirstOrDefault(p => p.BusRunId == x.Id && p.OrderIndex == routeStation.OrderIndex);

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

                var stationOrderMap = stations
                    .ToDictionary(s => s.StationId, s => s.OrderIndex);

                return new BusTripProgressDriverScheduleDto
                {
                    BusRunId = x.Id,
                    BusId = x.BusId,
                    BusLabel = ResolveBusLabel(x.Bus),
                    RouteId = x.RouteId,
                    RouteName = x.Route.Name,
                    RideDate = selectedDate,
                    StartTime = x.StartTime,
                    ShiftType = x.Status,
                    IsRunningNow = IsRunActiveNow(x, runs, selectedTime),
                    IsUpcoming = selectedTime < x.StartTime,
                    IsCompleted = IsRunCompleted(x, runs, selectedTime),
                    IsRecommended = x.Id == recommendedRunId,
                    Students = BuildDisplayStudentsForRun(
                            x,
                            selectedDate,
                            runStudentAssignments,
                            attendances)
                        .Select(a =>
                        {
                            var checkedInOnThisBus = HasCheckedInOnThisBus(attendances, x, a.StudentId);
                            var currentlyOnThisBus = IsCurrentlyOnThisBus(attendances, x, a.StudentId);
                            var activeOnOtherBus = GetActiveAttendanceOnOtherBus(attendances, selectedDate, x, a.StudentId);

                            return new BusTripProgressDriverScheduleStudentDto
                            {
                                StudentId = a.StudentId,
                                StudentCode = a.Student.StudentCode,
                                StudentName = a.Student.FullName,
                                StationId = a.Booking.StationId,
                                StationName = a.Booking.Station?.Name,
                                PickupAddress = a.Booking.PickupAddress,
                                PickupLatitude = a.Booking.Latitude,
                                PickupLongitude = a.Booking.Longitude,
                                HasCheckedInOnThisBus = checkedInOnThisBus,
                                IsCurrentlyOnThisBus = currentlyOnThisBus,
                                CurrentBusId = activeOnOtherBus?.BusId,
                                CurrentBusLabel = activeOnOtherBus?.Bus != null
                                    ? ResolveBusLabel(activeOnOtherBus.Bus)
                                    : null,
                                IsOnDifferentBusThanAssigned = activeOnOtherBus != null
                            };
                        })
                        .OrderBy(a =>
                            a.StationId.HasValue && stationOrderMap.TryGetValue(a.StationId.Value, out var orderIndex)
                                ? orderIndex
                                : int.MaxValue)
                        .ThenBy(a => a.PickupLatitude ?? double.MaxValue)
                        .ThenBy(a => a.PickupLongitude ?? double.MaxValue)
                        .ThenBy(a => a.StudentName)
                        .ToList(),
                    Stations = stations
                };
            }).ToList();
        }

        private IQueryable<BusRun> GetBusRunQueryable()
        {
            return _busRunRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Route)
                .ThenInclude(x => x.Campus)
                .Include(x => x.Driver)
                .Include(x => x.Teacher);
        }

        private IQueryable<BusTripProgress> GetProgressQueryable()
        {
            return _progressRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.Station)
                .Include(x => x.Bus)
                .Include(x => x.BusRun);
        }

        private static List<BusRunStudent> BuildDisplayStudentsForRun(
            BusRun run,
            DateTime rideDate,
            List<BusRunStudent> allAssignments,
            List<Attendance> attendanceRows)
        {
            var assignedStudents = allAssignments
                .Where(x => x.BusRunId == run.Id)
                .GroupBy(x => x.StudentId)
                .Select(x => x.OrderBy(y => y.Id).First())
                .ToList();

            var activeStudentIdsOnThisBus = attendanceRows
                .Where(a =>
                    a.Date.Date == rideDate &&
                    a.BusId == run.BusId &&
                    a.CheckInTime.HasValue &&
                    !a.CheckOutTime.HasValue)
                .Select(a => a.StudentId)
                .Distinct()
                .ToList();

            var additionalStudentsOnThisBus = allAssignments
                .Where(x =>
                    x.BusRunId != run.Id &&
                    x.BusRun.RouteId == run.RouteId &&
                    x.BusRun.ServiceDate.Date == rideDate &&
                    x.BusRun.StartTime == run.StartTime &&
                    activeStudentIdsOnThisBus.Contains(x.StudentId))
                .GroupBy(x => x.StudentId)
                .Select(x => x.OrderBy(y => y.Id).First())
                .Where(x => assignedStudents.All(y => y.StudentId != x.StudentId))
                .ToList();

            return assignedStudents
                .Concat(additionalStudentsOnThisBus)
                .OrderBy(x => x.Student.StudentCode)
                .ThenBy(x => x.Student.FullName)
                .ToList();
        }

        private static bool HasCheckedInOnThisBus(List<Attendance> attendanceRows, BusRun run, long studentId)
        {
            return attendanceRows.Any(a =>
                a.StudentId == studentId &&
                a.BusId == run.BusId &&
                a.Date.Date == run.ServiceDate.Date &&
                a.CheckInTime.HasValue);
        }

        private static bool IsCurrentlyOnThisBus(List<Attendance> attendanceRows, BusRun run, long studentId)
        {
            return attendanceRows.Any(a =>
                a.StudentId == studentId &&
                a.BusId == run.BusId &&
                a.Date.Date == run.ServiceDate.Date &&
                a.CheckInTime.HasValue &&
                !a.CheckOutTime.HasValue);
        }

        private static Attendance? GetActiveAttendanceOnOtherBus(
            List<Attendance> attendanceRows,
            DateTime rideDate,
            BusRun run,
            long studentId)
        {
            return attendanceRows
                .Where(a => a.StudentId == studentId && a.Date.Date == rideDate)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault(a =>
                    a.BusId != run.BusId &&
                    a.CheckInTime.HasValue &&
                    !a.CheckOutTime.HasValue);
        }

        private async Task<Bus> ValidateBusAsync(long busId)
        {
            if (busId <= 0)
                throw new Exception("BusId phai lon hon 0");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == busId)
                ?? throw new Exception("Bus khong ton tai");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bus dang khong hoat dong");

            return bus;
        }

        private async Task<BusRun> ValidateBusRunAsync(long busId, long busRunId, DateTime rideDate)
        {
            await ValidateBusAsync(busId);

            if (busRunId <= 0)
                throw new Exception("BusRunId phai lon hon 0");

            var busRun = await GetBusRunQueryable()
                .FirstOrDefaultAsync(x => x.Id == busRunId)
                ?? throw new Exception($"Khong tim thay chuyen xe voi BusRunId = {busRunId}");

            if (busRun.BusId != busId)
                throw new Exception($"BusRunId = {busRunId} thuoc xe {busRun.BusId}, khong phai xe {busId}");

            if (busRun.ServiceDate.Date != rideDate.Date)
                throw new Exception($"BusRunId = {busRunId} khong thuoc ngay {rideDate:yyyy-MM-dd}");

            return busRun;
        }

        private async Task<List<BusRouteStation>> GetRouteStationsAsync(long routeId)
        {
            var routeStations = await _routeStationRepo.Get()
                .Include(x => x.Station)
                .Where(x => x.RouteId == routeId)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();

            if (!routeStations.Any())
                throw new Exception("Tuyen xe chua co danh sach tram");

            var disabledStation = routeStations.FirstOrDefault(x => !x.Station.IsEnabled);
            if (disabledStation != null)
                throw new Exception($"Tram '{disabledStation.Station.Name}' dang khong hoat dong");

            return routeStations;
        }

        private async Task<List<BusTripProgress>> GetTripProgressListAsync(long busRunId, DateTime rideDate)
        {
            return await GetProgressQueryable()
                .Where(x => x.BusRunId == busRunId && x.RideDate.Date == rideDate.Date)
                .OrderBy(x => x.OrderIndex)
                .ThenBy(x => x.ArrivedAt)
                .ToListAsync();
        }

        private static BusRouteStation ResolveExpectedStation(List<BusRouteStation> routeStations, BusTripProgress? latestProgress)
        {
            if (latestProgress == null)
                return routeStations[0];

            var expectedStation = routeStations.FirstOrDefault(x => x.OrderIndex == latestProgress.OrderIndex + 1);
            if (expectedStation == null)
                throw new Exception("Xe da xac nhan het tat ca cac tram cua tuyen");

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

        private static bool AttendanceMatchesRun(Attendance attendance, BusRun run, List<BusRun> sameDayRuns)
        {
            var nextStartTime = ResolveNextRunStartTime(run, sameDayRuns);

            return IsTimeWithinRun(attendance.CheckInTime, run.StartTime, nextStartTime) ||
                   IsTimeWithinRun(attendance.CheckOutTime, run.StartTime, nextStartTime);
        }

        private static (DateTime? ActualStartAt, DateTime? ActualEndAt) ResolveActualTimeline(
            DateTime rideDate,
            BusRun run,
            List<BusTripProgress> tripProgress,
            List<Attendance> tripAttendances,
            List<BusRun> sameDayRuns)
        {
            if (tripProgress.Any())
                return (tripProgress.Min(x => x.ArrivedAt), tripProgress.Max(x => x.ArrivedAt));

            var actualTimes = tripAttendances
                .SelectMany(x => ResolveAttendanceTimes(rideDate, run, x, sameDayRuns))
                .OrderBy(x => x)
                .ToList();

            if (!actualTimes.Any())
                return (null, null);

            return (actualTimes.First(), actualTimes.Last());
        }

        private static IEnumerable<DateTime> ResolveAttendanceTimes(
            DateTime rideDate,
            BusRun run,
            Attendance attendance,
            List<BusRun> sameDayRuns)
        {
            var nextStartTime = ResolveNextRunStartTime(run, sameDayRuns);

            if (IsTimeWithinRun(attendance.CheckInTime, run.StartTime, nextStartTime))
                yield return rideDate.Date.Add(attendance.CheckInTime!.Value);

            if (IsTimeWithinRun(attendance.CheckOutTime, run.StartTime, nextStartTime))
                yield return rideDate.Date.Add(attendance.CheckOutTime!.Value);
        }

        private static bool IsRunActiveNow(BusRun run, List<BusRun> sameDayRuns, TimeSpan selectedTime)
        {
            var nextStartTime = ResolveNextRunStartTime(run, sameDayRuns);
            return selectedTime >= run.StartTime &&
                   (!nextStartTime.HasValue || selectedTime < nextStartTime.Value);
        }

        private static bool IsRunCompleted(BusRun run, List<BusRun> sameDayRuns, TimeSpan selectedTime)
        {
            var nextStartTime = ResolveNextRunStartTime(run, sameDayRuns);
            return nextStartTime.HasValue && selectedTime >= nextStartTime.Value;
        }

        private static TimeSpan? ResolveNextRunStartTime(BusRun run, List<BusRun> sameDayRuns)
        {
            return sameDayRuns
                .Where(x =>
                    x.BusId == run.BusId &&
                    x.ServiceDate.Date == run.ServiceDate.Date &&
                    x.StartTime > run.StartTime)
                .OrderBy(x => x.StartTime)
                .Select(x => (TimeSpan?)x.StartTime)
                .FirstOrDefault();
        }

        private static bool IsTimeWithinRun(TimeSpan? candidateTime, TimeSpan startTime, TimeSpan? nextStartTime)
        {
            if (!candidateTime.HasValue)
                return false;

            return candidateTime.Value >= startTime &&
                   (!nextStartTime.HasValue || candidateTime.Value < nextStartTime.Value);
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

        private static string ResolveBusLabel(Bus bus)
        {
            return !string.IsNullOrWhiteSpace(bus.BusNumber)
                ? bus.BusNumber
                : bus.LicensePlate;
        }

        private static BusTripProgressEventDto MapEvent(BusTripProgress progress)
        {
            return new BusTripProgressEventDto
            {
                Id = progress.Id,
                BusId = progress.BusId,
                BusRunId = progress.BusRunId,
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
