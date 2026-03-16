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
                query = query.Where(x =>
                    x.Name.ToLower().Contains(keyword) ||
                    (x.Address != null && x.Address.ToLower().Contains(keyword)) ||
                    (x.Description != null && x.Description.ToLower().Contains(keyword)));
            }

            var totalItems = await query.CountAsync();

            var stations = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BusStationDto>
            {
                Items = stations.Select(MapToDto).ToList(),
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

            return MapToDto(station);
        }

        public async Task CreateBusStationAsync(BusStationCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Tên bus station không được để trống");

            var station = new BusStation
            {
                Name = dto.Name.Trim(),
                Address = NormalizeOptional(dto.Address),
                Description = NormalizeOptional(dto.Description),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsEnabled = dto.IsEnabled ?? true
            };

            await _stationRepo.AddAsync(station);
            await _stationRepo.SaveChangesAsync();
        }

        public async Task<BusStationDto> UpdateBusStationAsync(long id, BusStationUpdateDto dto)
        {
            var station = await _stationRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus station không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                station.Name = dto.Name.Trim();

            if (dto.Address != null)
                station.Address = NormalizeOptional(dto.Address);

            if (dto.Description != null)
                station.Description = NormalizeOptional(dto.Description);

            if (dto.Latitude.HasValue)
                station.Latitude = dto.Latitude;

            if (dto.Longitude.HasValue)
                station.Longitude = dto.Longitude;

            if (dto.IsEnabled.HasValue)
                station.IsEnabled = dto.IsEnabled.Value;

            _stationRepo.Update(station);
            await _stationRepo.SaveChangesAsync();

            return MapToDto(station);
        }

        public async Task DeleteBusStationAsync(long id)
        {
            var station = await _stationRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus station không tồn tại");

            _stationRepo.Delete(station);
            await _stationRepo.SaveChangesAsync();
        }

        private static BusStationDto MapToDto(BusStation station)
        {
            return new BusStationDto
            {
                Id = station.Id,
                Name = station.Name,
                Address = station.Address,
                Description = station.Description,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                IsEnabled = station.IsEnabled
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
