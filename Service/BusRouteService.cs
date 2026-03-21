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
            var query = _routeRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(keyword) ||
                    x.Campus.Name.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();

            var routes = await query.Include(x => x.Campus)
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

        public async Task<BusRouteDto> GetBusRouteByIdAsync(long id)
        {
            var route = await GetRouteQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus route khong ton tai");

            return MapToDto(route);
        }

        public async Task<BusRouteDto> CreateBusRouteAsync(BusRouteCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Ten tuyen khong duoc de trong");

            if (!dto.StationIds.Any())
                throw new Exception("Tuyen xe phai co it nhat mot tram");

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
                ?? throw new Exception("Bus route khong ton tai");

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
                ?? throw new Exception("Bus route khong ton tai");

            return MapToDto(updatedRoute);
        }

        public async Task DeleteBusRouteAsync(long id)
        {
            var route = await _routeRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus route khong ton tai");

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

        private async Task<Campus> ValidateCampusAsync(long campusId)
        {
            if (campusId <= 0)
                throw new Exception("CampusId phai lon hon 0");

            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == campusId)
                ?? throw new Exception("Campus khong ton tai");

            if (!campus.IsActive)
                throw new Exception("Campus dang khong hoat dong");

            return campus;
        }

        private static List<long> ValidateStationIds(List<long> stationIds)
        {
            if (!stationIds.Any())
                throw new Exception("Tuyen xe phai co it nhat mot tram");

            if (stationIds.Any(x => x <= 0))
                throw new Exception("StationIds phai lon hon 0");

            var distinctStationIds = stationIds.Distinct().ToList();
            if (distinctStationIds.Count != stationIds.Count)
                throw new Exception("Danh sach tram bi trung");

            return distinctStationIds;
        }

        private async Task<List<BusStation>> ValidateStationsAsync(List<long> stationIds)
        {
            var stations = await _stationRepo.Get()
                .Where(x => stationIds.Contains(x.Id))
                .ToListAsync();

            if (stations.Count != stationIds.Count)
                throw new Exception("Mot hoac nhieu tram khong ton tai");

            var disabledStation = stations.FirstOrDefault(x => !x.IsEnabled);
            if (disabledStation != null)
                throw new Exception($"Bus station '{disabledStation.Name}' dang khong hoat dong");

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
                        StationId = x.StationId,
                        StationName = x.Station.Name,
                        OrderIndex = x.OrderIndex
                    })
                    .ToList()
            };
        }
    }
}
