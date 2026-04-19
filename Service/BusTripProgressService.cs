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

        public BusTripProgressService(
            IRepository<BusTripProgress> progressRepo,
            IRepository<Bus> busRepo,
            IRepository<BusAssignment> busAssignmentRepo,
            IRepository<BusSchedule> busScheduleRepo,
            IRepository<BusRouteStation> routeStationRepo)
        {
            _progressRepo = progressRepo;
            _busRepo = busRepo;
            _busAssignmentRepo = busAssignmentRepo;
            _busScheduleRepo = busScheduleRepo;
            _routeStationRepo = routeStationRepo;
        }

        public async Task<BusTripProgressEventDto> MarkArrivedAsync(BusTripProgressArriveDto dto)
        {
            var arrivedAt = dto.ArrivedAt ?? DateTime.UtcNow;
            var schedule = await ValidateScheduleAsync(dto.BusId, dto.BusScheduleId, arrivedAt.Date);
            var routeStations = await GetRouteStationsAsync(schedule.RouteId);
            var targetStation = routeStations
                .FirstOrDefault(x => x.StationId == dto.StationId)
                ?? throw new Exception("Trạm không thuộc tuyến của lịch chạy này");

            var progressList = await GetTripProgressListAsync(schedule.Id, arrivedAt.Date);
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
                RideDate = arrivedAt.Date,
                OrderIndex = targetStation.OrderIndex,
                ArrivedAt = arrivedAt
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

            var selectedDate = (rideDate ?? DateTime.UtcNow).Date;
            var selectedTime = atTime ?? DateTime.UtcNow.TimeOfDay;
            var dayOfWeek = (int)selectedDate.DayOfWeek;

            var assignment = await _busAssignmentRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Driver)
                .FirstOrDefaultAsync(x => x.DriverId == driverId)
                ?? throw new Exception("Tài xế chưa được phân công xe");

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
                throw new Exception("Tài xế không có lịch chạy nào trong ngày đã chọn");

            var runningSchedules = schedules
                .Where(x => x.StartTime <= selectedTime && x.EndTime >= selectedTime)
                .ToList();

            var recommendedScheduleId = runningSchedules.Count == 1
                ? runningSchedules[0].Id
                : schedules.FirstOrDefault(x => x.StartTime > selectedTime)?.Id
                    ?? schedules.Last().Id;

            return schedules.Select(x =>
            {
                var busLabel = !string.IsNullOrWhiteSpace(assignment.Bus.BusNumber)
                    ? assignment.Bus.BusNumber
                    : assignment.Bus.LicensePlate;

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
                    IsRecommended = x.Id == recommendedScheduleId
                };
            }).ToList();
        }

        public async Task<BusTripProgressCurrentDto> GetCurrentAsync(long busId, long busScheduleId, DateTime? rideDate)
        {
            var selectedDate = (rideDate ?? DateTime.UtcNow).Date;
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
                            OrderIndex = routeStation.OrderIndex,
                            IsVisited = progress != null,
                            ArrivedAt = progress?.ArrivedAt
                        };
                    })
                    .ToList()
            };
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

            var dayOfWeek = (int)rideDate.DayOfWeek;

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
