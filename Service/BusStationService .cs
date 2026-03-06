using BE_API.Dto.BusStation;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusStationService : IBusStationService
    {
        private readonly IRepository<BusStation> _stationRepo;

        public BusStationService(IRepository<BusStation> stationRepo)
        {
            _stationRepo = stationRepo;
        }

        public async Task<PagedResult<BusStationDto>> SearchBusStationAsync(string? keyword, int page, int pageSize)
        {
            IQueryable<BusStation> query = _stationRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();

            var stations = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = stations.Select(x => new BusStationDto
            {
                Id = x.Id,
                Name = x.Name,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                IsEnabled = x.IsEnabled
            }).ToList();

            return new PagedResult<BusStationDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<BusStationDto> GetBusStationByIdAsync(long id)
        {
            var station = await _stationRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus station không tồn tại");

            return new BusStationDto
            {
                Id = station.Id,
                Name = station.Name,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                IsEnabled = station.IsEnabled
            };
        }

        public async Task CreateBusStationAsync(BusStationCreateDto dto)
        {
            var station = new BusStation
            {
                Name = dto.Name,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsEnabled = true
            };

            await _stationRepo.AddAsync(station);
        }

        public async Task<BusStationDto> UpdateBusStationAsync(long id, BusStationUpdateDto dto)
        {
            var station = await _stationRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus station không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                station.Name = dto.Name;

            if (dto.Latitude.HasValue)
                station.Latitude = dto.Latitude;

            if (dto.Longitude.HasValue)
                station.Longitude = dto.Longitude;

            if (dto.IsEnabled.HasValue)
                station.IsEnabled = dto.IsEnabled.Value;

            _stationRepo.Update(station);

            return new BusStationDto
            {
                Id = station.Id,
                Name = station.Name,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                IsEnabled = station.IsEnabled
            };
        }

        public async Task DeleteBusStationAsync(long id)
        {
            var station = await _stationRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus station không tồn tại");

            _stationRepo.Delete(station);
        }
    }
}
