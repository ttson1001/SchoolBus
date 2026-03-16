using BE_API.Dto.Bus;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusService : IBusService
    {
        private readonly IRepository<Bus> _busRepo;

        public BusService(IRepository<Bus> busRepo)
        {
            _busRepo = busRepo;
        }

        public async Task CreateBusAsync(BusCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.LicensePlate))
                throw new Exception("Biển số xe không được để trống");

            var bus = new Bus
            {
                LicensePlate = dto.LicensePlate.Trim(),
                Capacity = dto.Capacity,
                IsEnabled = dto.IsEnabled ?? true,
                BusNumber = NormalizeOptional(dto.BusNumber),
                ImageUrl = NormalizeOptional(dto.ImageUrl),
                Color = NormalizeOptional(dto.Color),
                BusType = NormalizeOptional(dto.BusType)
            };

            await _busRepo.AddAsync(bus);
            await _busRepo.SaveChangesAsync();
        }

        public async Task<BusDto> GetBusByIdAsync(long id)
        {
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus không tồn tại");

            return MapToDto(bus);
        }

        public async Task<BusDto> UpdateBusAsync(long id, BusUpdateDto dto)
        {
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.LicensePlate))
                bus.LicensePlate = dto.LicensePlate.Trim();

            if (dto.Capacity.HasValue)
                bus.Capacity = dto.Capacity.Value;

            if (dto.IsEnabled.HasValue)
                bus.IsEnabled = dto.IsEnabled.Value;

            if (dto.BusNumber != null)
                bus.BusNumber = NormalizeOptional(dto.BusNumber);

            if (dto.ImageUrl != null)
                bus.ImageUrl = NormalizeOptional(dto.ImageUrl);

            if (dto.Color != null)
                bus.Color = NormalizeOptional(dto.Color);

            if (dto.BusType != null)
                bus.BusType = NormalizeOptional(dto.BusType);

            _busRepo.Update(bus);
            await _busRepo.SaveChangesAsync();

            return MapToDto(bus);
        }

        public async Task DeleteBusAsync(long id)
        {
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus không tồn tại");

            _busRepo.Delete(bus);
            await _busRepo.SaveChangesAsync();
        }

        public async Task<PagedResult<BusDto>> SearchBusAsync(string? keyword, int page, int pageSize)
        {
            var query = _busRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.LicensePlate.ToLower().Contains(keyword) ||
                    (x.BusNumber != null && x.BusNumber.ToLower().Contains(keyword)) ||
                    (x.Color != null && x.Color.ToLower().Contains(keyword)) ||
                    (x.BusType != null && x.BusType.ToLower().Contains(keyword)));
            }

            var totalItems = await query.CountAsync();

            var buses = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = buses.Select(MapToDto).ToList();

            return new PagedResult<BusDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        private static BusDto MapToDto(Bus bus)
        {
            return new BusDto
            {
                Id = bus.Id,
                LicensePlate = bus.LicensePlate,
                Capacity = bus.Capacity,
                IsEnabled = bus.IsEnabled,
                BusNumber = bus.BusNumber,
                ImageUrl = bus.ImageUrl,
                Color = bus.Color,
                BusType = bus.BusType
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
