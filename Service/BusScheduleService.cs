using BE_API.Common;
using BE_API.Dto.BusSchedule;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusScheduleService : IBusScheduleService
    {
        private readonly IRepository<BusSchedule> _busScheduleRepo;
        private readonly IRepository<Bus> _busRepo;
        private readonly IRepository<BusRoute> _busRouteRepo;
        private readonly IRepository<Campus> _campusRepo;
        private readonly IAppTime _appTime;

        public BusScheduleService(
            IRepository<BusSchedule> busScheduleRepo,
            IRepository<Bus> busRepo,
            IRepository<BusRoute> busRouteRepo,
            IRepository<Campus> campusRepo,
            IAppTime appTime)
        {
            _busScheduleRepo = busScheduleRepo;
            _busRepo = busRepo;
            _busRouteRepo = busRouteRepo;
            _campusRepo = campusRepo;
            _appTime = appTime;
        }

        public async Task<BusScheduleDto> CreateBusScheduleAsync(BusScheduleCreateDto dto)
        {
            var bus = await ValidateBusAsync(dto.BusId);
            var route = await ValidateRouteAsync(dto.RouteId);
            var normalized = NormalizeSchedule(
                dto.StartDate,
                dto.EndDate,
                dto.StartTime,
                dto.EndTime,
                dto.DayOfWeek,
                dto.ShiftType,
                true);

            await EnsureBusScheduleNotOverlappedAsync(
                dto.BusId,
                normalized.startDate,
                normalized.endDate,
                normalized.startTime,
                normalized.endTime,
                normalized.dayOfWeek);

            var schedule = new BusSchedule
            {
                BusId = bus.Id,
                RouteId = route.Id,
                StartDate = normalized.startDate,
                EndDate = normalized.endDate,
                StartTime = normalized.startTime,
                EndTime = normalized.endTime,
                DayOfWeek = normalized.dayOfWeek,
                ShiftType = normalized.shiftType,
                IsActive = true
            };

            await _busScheduleRepo.AddAsync(schedule);
            await _busScheduleRepo.SaveChangesAsync();

            var createdSchedule = await GetScheduleQueryable()
                .FirstOrDefaultAsync(x => x.Id == schedule.Id)
                ?? throw new Exception("Bus schedule không tồn tại");

            return MapToDto(createdSchedule);
        }

        public async Task<BusScheduleDto> GetBusScheduleByIdAsync(long id)
        {
            var schedule = await GetScheduleQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus schedule không tồn tại");

            return MapToDto(schedule);
        }

        public async Task<List<BusScheduleDto>> SearchBusSchedulesAsync(long? busId, long? routeId, long? campusId, DateTime? fromDate, DateTime? toDate)
        {
            if (busId.HasValue)
                await ValidateBusAsync(busId.Value);

            if (routeId.HasValue)
                await ValidateRouteAsync(routeId.Value);

            if (campusId.HasValue)
                await ValidateCampusAsync(campusId.Value);

            var normalizedFromDate = fromDate?.Date;
            var normalizedToDate = toDate?.Date;

            if (normalizedFromDate.HasValue && normalizedToDate.HasValue && normalizedFromDate.Value > normalizedToDate.Value)
                throw new Exception("Từ ngày phải nhỏ hơn hoặc bằng đến ngày");

            var query = GetScheduleQueryable();

            if (busId.HasValue)
                query = query.Where(x => x.BusId == busId.Value);

            if (routeId.HasValue)
                query = query.Where(x => x.RouteId == routeId.Value);

            if (campusId.HasValue)
                query = query.Where(x => x.Route.CampusId == campusId.Value);

            if (normalizedFromDate.HasValue && normalizedToDate.HasValue)
            {
                query = query.Where(x =>
                    x.StartDate.Date <= normalizedToDate.Value &&
                    (!x.EndDate.HasValue || x.EndDate.Value.Date >= normalizedFromDate.Value));
            }
            else if (normalizedFromDate.HasValue)
            {
                query = query.Where(x =>
                    !x.EndDate.HasValue || x.EndDate.Value.Date >= normalizedFromDate.Value);
            }
            else if (normalizedToDate.HasValue)
            {
                query = query.Where(x => x.StartDate.Date <= normalizedToDate.Value);
            }

            var schedules = await query
                .OrderBy(x => x.Route.CampusId)
                .ThenBy(x => x.RouteId)
                .ThenBy(x => x.BusId)
                .ThenBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<List<BusScheduleDto>> GetBusSchedulesByBusIdAsync(long busId)
        {
            await ValidateBusAsync(busId);

            var schedules = await GetScheduleQueryable()
                .Where(x => x.BusId == busId)
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<List<BusScheduleDto>> GetBusSchedulesByRouteIdAsync(long routeId)
        {
            await ValidateRouteAsync(routeId);

            var schedules = await GetScheduleQueryable()
                .Where(x => x.RouteId == routeId)
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<List<BusScheduleDto>> GetBusSchedulesByCampusIdAsync(long campusId)
        {
            await ValidateCampusAsync(campusId);

            var schedules = await GetScheduleQueryable()
                .Where(x => x.Route.CampusId == campusId)
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<List<BusScheduleDto>> GetBusSchedulesAtTimeAsync(DateTime atTime, long? campusId)
        {
            if (campusId.HasValue)
                await ValidateCampusAsync(campusId.Value);

            var date = atTime.Date;
            var time = atTime.TimeOfDay;
            var dayOfWeek = ScheduleDayOfWeek.FromDate(atTime);

            var query = GetScheduleQueryable()
                .Where(x =>
                    x.IsActive &&
                    x.DayOfWeek == dayOfWeek &&
                    x.StartDate.Date <= date &&
                    (!x.EndDate.HasValue || x.EndDate.Value.Date >= date) &&
                    x.StartTime <= time &&
                    x.EndTime >= time);

            if (campusId.HasValue)
                query = query.Where(x => x.Route.CampusId == campusId.Value);

            var schedules = await query
                .OrderBy(x => x.Route.CampusId)
                .ThenBy(x => x.Route.Name)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<BusScheduleDto> UpdateBusScheduleAsync(long id, BusScheduleUpdateDto dto)
        {
            var schedule = await _busScheduleRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus schedule không tồn tại");

            if (dto.BusId.HasValue)
            {
                await ValidateBusAsync(dto.BusId.Value);
                schedule.BusId = dto.BusId.Value;
            }

            if (dto.RouteId.HasValue)
            {
                await ValidateRouteAsync(dto.RouteId.Value);
                schedule.RouteId = dto.RouteId.Value;
            }

            if (dto.StartDate.HasValue)
                schedule.StartDate = dto.StartDate.Value.Date;

            if (dto.EndDate.HasValue)
                schedule.EndDate = dto.EndDate.Value.Date;

            if (dto.StartTime.HasValue)
                schedule.StartTime = dto.StartTime.Value;

            if (dto.EndTime.HasValue)
                schedule.EndTime = dto.EndTime.Value;

            if (dto.DayOfWeek.HasValue)
                schedule.DayOfWeek = dto.DayOfWeek.Value;

            if (dto.ShiftType != null)
                schedule.ShiftType = NormalizeShiftType(dto.ShiftType);

            if (dto.IsActive.HasValue)
                schedule.IsActive = dto.IsActive.Value;

            NormalizeSchedule(
                schedule.StartDate,
                schedule.EndDate,
                schedule.StartTime,
                schedule.EndTime,
                schedule.DayOfWeek,
                schedule.ShiftType);

            await EnsureBusScheduleNotOverlappedAsync(
                schedule.BusId,
                schedule.StartDate,
                schedule.EndDate,
                schedule.StartTime,
                schedule.EndTime,
                schedule.DayOfWeek,
                schedule.Id);

            _busScheduleRepo.Update(schedule);
            await _busScheduleRepo.SaveChangesAsync();

            var updatedSchedule = await GetScheduleQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus schedule không tồn tại");

            return MapToDto(updatedSchedule);
        }

        public async Task DeleteBusScheduleAsync(long id)
        {
            var schedule = await _busScheduleRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus schedule không tồn tại");

            _busScheduleRepo.Delete(schedule);
            await _busScheduleRepo.SaveChangesAsync();
        }

        private IQueryable<BusSchedule> GetScheduleQueryable()
        {
            return _busScheduleRepo.Get()
                .Include(x => x.Bus)
                .Include(x => x.Route)
                .ThenInclude(x => x.Campus);
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

        private async Task<BusRoute> ValidateRouteAsync(long routeId)
        {
            if (routeId <= 0)
                throw new Exception("RouteId phải lớn hơn 0");

            var route = await _busRouteRepo.Get()
                .Include(x => x.Campus)
                .FirstOrDefaultAsync(x => x.Id == routeId)
                ?? throw new Exception("Bus route không tồn tại");

            if (!route.IsEnabled)
                throw new Exception("Bus route đang không hoạt động");

            if (!route.Campus.IsActive)
                throw new Exception($"Campus '{route.Campus.Name}' đang không hoạt động");

            return route;
        }

        private async Task ValidateCampusAsync(long campusId)
        {
            if (campusId <= 0)
                throw new Exception("CampusId phải lớn hơn 0");

            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == campusId)
                ?? throw new Exception("Campus không tồn tại");

            if (!campus.IsActive)
                throw new Exception("Campus đang không hoạt động");
        }

        private async Task EnsureBusScheduleNotOverlappedAsync(
            long busId,
            DateTime startDate,
            DateTime? endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int dayOfWeek,
            long? excludedId = null)
        {
            var overlap = await _busScheduleRepo.Get()
                .Where(x =>
                    x.BusId == busId &&
                    x.DayOfWeek == dayOfWeek &&
                    (!excludedId.HasValue || x.Id != excludedId.Value) &&
                    x.StartDate.Date <= (endDate ?? DateTime.MaxValue).Date &&
                    (!x.EndDate.HasValue || x.EndDate.Value.Date >= startDate.Date) &&
                    x.StartTime < endTime &&
                    x.EndTime > startTime)
                .AnyAsync();

            if (overlap)
                throw new Exception("Bus đã có lịch bị trùng khung giờ");
        }

        private static (DateTime startDate, DateTime? endDate, TimeSpan startTime, TimeSpan endTime, int dayOfWeek, string shiftType)
            NormalizeSchedule(
                DateTime startDate,
                DateTime? endDate,
                TimeSpan startTime,
                TimeSpan endTime,
                int dayOfWeek,
                string shiftType,
                bool requireFutureStartDate = false)
        {
            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate?.Date;
            var today = _appTime.TodayDate;

            if (requireFutureStartDate && normalizedStartDate <= today)
                throw new Exception("StartDate phải lớn hơn ngày hiện tại");

            if (normalizedEndDate.HasValue && normalizedEndDate.Value < normalizedStartDate)
                throw new Exception("EndDate phải lớn hơn hoặc bằng StartDate");

            if (startTime >= endTime)
                throw new Exception("EndTime phải lớn hơn StartTime");

            if (dayOfWeek < 0 || dayOfWeek > 6)
                throw new Exception("DayOfWeek chỉ nhận giá trị từ 0 đến 6");

            return (
                normalizedStartDate,
                normalizedEndDate,
                startTime,
                endTime,
                dayOfWeek,
                NormalizeShiftType(shiftType));
        }

        private static string NormalizeShiftType(string? shiftType)
        {
            if (string.IsNullOrWhiteSpace(shiftType))
                throw new Exception("ShiftType không được để trống");

            var normalizedShiftType = shiftType.Trim().ToUpperInvariant();
            var allowedShiftTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PICKUP",
                "DROPOFF",
                "ROUNDTRIP"
            };

            if (!allowedShiftTypes.Contains(normalizedShiftType))
                throw new Exception("ShiftType chỉ chấp nhận PICKUP, DROPOFF hoặc ROUNDTRIP");

            return normalizedShiftType;
        }

        private static BusScheduleDto MapToDto(BusSchedule schedule)
        {
            var busLabel = !string.IsNullOrWhiteSpace(schedule.Bus.BusNumber)
                ? schedule.Bus.BusNumber
                : schedule.Bus.LicensePlate;

            return new BusScheduleDto
            {
                Id = schedule.Id,
                BusId = schedule.BusId,
                BusLabel = busLabel,
                RouteId = schedule.RouteId,
                RouteName = schedule.Route.Name,
                CampusId = schedule.Route.CampusId,
                CampusName = schedule.Route.Campus.Name,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                DayOfWeek = schedule.DayOfWeek,
                ShiftType = schedule.ShiftType,
                IsActive = schedule.IsActive
            };
        }
    }
}
