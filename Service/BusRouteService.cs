using BE_API.Dto.BusRoute;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusRouteService : IBusRouteService
    {
        private readonly IRepository<BusRoute> _routeRepo;
        private readonly IRepository<BusRouteStation> _routeStationRepo;
        private readonly IRepository<Campus> _campusRepo;
        private readonly IRepository<BusStation> _stationRepo;

        public BusRouteService(
            IRepository<BusRoute> routeRepo,
            IRepository<BusRouteStation> routeStationRepo,
            IRepository<Campus> campusRepo,
            IRepository<BusStation> stationRepo)
        {
            _routeRepo = routeRepo;
            _routeStationRepo = routeStationRepo;
            _campusRepo = campusRepo;
            _stationRepo = stationRepo;
        }

        public async Task<PagedResult<BusRouteDto>> SearchBusRouteAsync(string? keyword, int page, int pageSize)
        {
            var query = BuildSearchQuery(keyword, null, null);
            return await BuildPagedResultAsync(query, page, pageSize);
        }

        public async Task<PagedResult<BusRouteDto>> GetActiveBusRouteAsync(string? keyword, long? campusId, int page, int pageSize)
        {
            var query = BuildSearchQuery(keyword, campusId, true);
            return await BuildPagedResultAsync(query, page, pageSize);
        }

        public async Task<BusRouteDto> GetBusRouteByIdAsync(long id)
        {
            var route = await GetRouteQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus route không tồn tại");

            return MapToDto(route);
        }

        public async Task<BusRouteDto> CreateBusRouteAsync(BusRouteCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Tên tuyến không được để trống");

            if (!dto.StationIds.Any())
                throw new Exception("Tuyến xe phải có ít nhất một trạm");

            var campus = await ValidateCampusAsync(dto.CampusId);
            var stationIds = ValidateStationIds(dto.StationIds);
            var stations = await ValidateStationsAsync(stationIds);

            var route = new BusRoute
            {
                Name = dto.Name.Trim(),
                CampusId = campus.Id,
                IsEnabled = true
            };

            await _routeRepo.AddAsync(route);
            await _routeRepo.SaveChangesAsync();

            var routeStations = stationIds
                .Select((stationId, index) => new BusRouteStation
                {
                    RouteId = route.Id,
                    StationId = stationId,
                    OrderIndex = index + 1
                })
                .ToList();

            await _routeStationRepo.AddRangeAsync(routeStations);
            await _routeStationRepo.SaveChangesAsync();

            route.Campus = campus;
            route.Stations = routeStations
                .Select(x =>
                {
                    x.Station = stations.First(s => s.Id == x.StationId);
                    return x;
                })
                .ToList();

            return MapToDto(route);
        }

        public async Task<BusRouteDto> UpdateBusRouteAsync(long id, BusRouteUpdateDto dto)
        {
            var route = await GetRouteQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus route không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                route.Name = dto.Name.Trim();

            if (dto.CampusId.HasValue)
            {
                var campus = await ValidateCampusAsync(dto.CampusId.Value);
                route.CampusId = campus.Id;
                route.Campus = campus;
            }

            if (dto.IsEnabled.HasValue)
                route.IsEnabled = dto.IsEnabled.Value;

            if (dto.StationIds != null)
            {
                var stationIds = ValidateStationIds(dto.StationIds);
                var stations = await ValidateStationsAsync(stationIds);

                var oldStations = await _routeStationRepo.Get()
                    .Where(x => x.RouteId == route.Id)
                    .ToListAsync();

                if (oldStations.Any())
                    _routeStationRepo.DeleteRange(oldStations);

                var newRouteStations = stationIds
                    .Select((stationId, index) => new BusRouteStation
                    {
                        RouteId = route.Id,
                        StationId = stationId,
                        OrderIndex = index + 1
                    })
                    .ToList();

                await _routeStationRepo.AddRangeAsync(newRouteStations);
                await _routeStationRepo.SaveChangesAsync();

                route.Stations = newRouteStations
                    .Select(x =>
                    {
                        x.Station = stations.First(s => s.Id == x.StationId);
                        return x;
                    })
                    .ToList();
            }

            _routeRepo.Update(route);
            await _routeRepo.SaveChangesAsync();

            var updatedRoute = await GetRouteQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus route không tồn tại");

            return MapToDto(updatedRoute);
        }

        public async Task DeleteBusRouteAsync(long id)
        {
            var route = await _routeRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus route không tồn tại");

            var routeStations = await _routeStationRepo.Get()
                .Where(x => x.RouteId == id)
                .ToListAsync();

            if (routeStations.Any())
                _routeStationRepo.DeleteRange(routeStations);

            _routeRepo.Delete(route);
            await _routeRepo.SaveChangesAsync();
        }

        private IQueryable<BusRoute> GetRouteQueryable()
        {
            return _routeRepo.Get()
                .Include(x => x.Campus)
                .Include(x => x.Stations.OrderBy(s => s.OrderIndex))
                .ThenInclude(x => x.Station);
        }

        private IQueryable<BusRoute> BuildSearchQuery(string? keyword, long? campusId, bool? isEnabled)
        {
            var query = _routeRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(keyword) ||
                    x.Campus.Name.ToLower().Contains(keyword));
            }

            if (campusId.HasValue)
                query = query.Where(x => x.CampusId == campusId.Value);

            if (isEnabled.HasValue)
                query = query.Where(x => x.IsEnabled == isEnabled.Value);

            return query.Include(x => x.Campus);
        }

        private static async Task<PagedResult<BusRouteDto>> BuildPagedResultAsync(IQueryable<BusRoute> query, int page, int pageSize)
        {
            var totalItems = await query.CountAsync();

            var routes = await query
                .Include(x => x.Stations)
                .ThenInclude(x => x.Station)
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BusRouteDto>
            {
                Items = routes.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<Campus> ValidateCampusAsync(long campusId)
        {
            if (campusId <= 0)
                throw new Exception("CampusId phải lớn hơn 0");

            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == campusId)
                ?? throw new Exception("Campus không tồn tại");

            if (!campus.IsActive)
                throw new Exception("Campus đang không hoạt động");

            return campus;
        }

        private static List<long> ValidateStationIds(List<long> stationIds)
        {
            if (!stationIds.Any())
                throw new Exception("Tuyến xe phải có ít nhất một trạm");

            if (stationIds.Any(x => x <= 0))
                throw new Exception("StationIds phải lớn hơn 0");

            var distinctStationIds = stationIds.Distinct().ToList();
            if (distinctStationIds.Count != stationIds.Count)
                throw new Exception("Danh sách trạm bị trùng");

            return distinctStationIds;
        }

        private async Task<List<BusStation>> ValidateStationsAsync(List<long> stationIds)
        {
            var stations = await _stationRepo.Get()
                .Where(x => stationIds.Contains(x.Id))
                .ToListAsync();

            if (stations.Count != stationIds.Count)
                throw new Exception("Một hoặc nhiều trạm không tồn tại");

            var disabledStation = stations.FirstOrDefault(x => !x.IsEnabled);
            if (disabledStation != null)
                throw new Exception($"Bus station '{disabledStation.Name}' đang không hoạt động");

            return stations;
        }

        private static BusRouteDto MapToDto(BusRoute route)
        {
            return new BusRouteDto
            {
                Id = route.Id,
                Name = route.Name,
                IsEnabled = route.IsEnabled,
                CampusId = route.CampusId,
                CampusName = route.Campus.Name,
                Stations = route.Stations
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => new BusRouteStationDto
                    {
                        Id = x.Station.Id,
                        Name = x.Station.Name,
                        Address = x.Station.Address,
                        Description = x.Station.Description,
                        Latitude = x.Station.Latitude,
                        Longitude = x.Station.Longitude,
                        IsEnabled = x.Station.IsEnabled,
                        OrderIndex = x.OrderIndex
                    })
                    .ToList()
            };
        }
    }
}
