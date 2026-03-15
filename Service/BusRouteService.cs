using BE_API.Dto.BusRoute;
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

        public async Task<BusRouteDto> CreateBusRouteAsync(BusRouteCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Tên tuyến không được để trống");

            if (!dto.StationIds.Any())
                throw new Exception("Tuyến xe phải có ít nhất một trạm");

            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == dto.CampusId)
                ?? throw new Exception("Campus không tồn tại");

            var stationIds = dto.StationIds.Distinct().ToList();
            if (stationIds.Count != dto.StationIds.Count)
                throw new Exception("Danh sách trạm bị trùng");

            var stations = await _stationRepo.Get()
                .Where(x => stationIds.Contains(x.Id))
                .ToListAsync();

            if (stations.Count != stationIds.Count)
                throw new Exception("Một hoặc nhiều trạm không tồn tại");

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

            return new BusRouteDto
            {
                Id = route.Id,
                Name = route.Name,
                IsEnabled = route.IsEnabled,
                CampusId = campus.Id,
                CampusName = campus.Name,
                Stations = routeStations
                    .Select(x => new BusRouteStationDto
                    {
                        StationId = x.StationId,
                        StationName = stations.First(s => s.Id == x.StationId).Name,
                        OrderIndex = x.OrderIndex
                    })
                    .ToList()
            };
        }
    }
}
