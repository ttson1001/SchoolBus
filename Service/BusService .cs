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
            var bus = new Bus
            {
                LicensePlate = dto.LicensePlate,
                Capacity = dto.Capacity,
                IsEnabled = true
            };

            await _busRepo.AddAsync(bus);
            await _busRepo.SaveChangesAsync();
        }

        public async Task<BusDto> GetBusByIdAsync(long id)
        {
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus not found");

            return new BusDto
            {
                Id = bus.Id,
                LicensePlate = bus.LicensePlate,
                Capacity = bus.Capacity,
                IsEnabled = bus.IsEnabled
            };
        }

        public async Task<BusDto> UpdateBusAsync(long id, BusUpdateDto dto)
        {
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus not found");

            if (!string.IsNullOrWhiteSpace(dto.LicensePlate))
                bus.LicensePlate = dto.LicensePlate;

            if (dto.Capacity.HasValue)
                bus.Capacity = dto.Capacity.Value;

            if (dto.IsEnabled.HasValue)
                bus.IsEnabled = dto.IsEnabled.Value;

            _busRepo.Update(bus);
            await _busRepo.SaveChangesAsync();

            return new BusDto
            {
                Id = bus.Id,
                LicensePlate = bus.LicensePlate,
                Capacity = bus.Capacity,
                IsEnabled = bus.IsEnabled
            };
        }

        public async Task DeleteBusAsync(long id)
        {
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus not found");

            _busRepo.Delete(bus);
            await _busRepo.SaveChangesAsync();
        }

        public async Task<PagedResult<BusDto>> SearchBusAsync(string? keyword, int page, int pageSize)
        {
            var query = _busRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x => x.LicensePlate.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();

            var buses = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = buses.Select(x => new BusDto
            {
                Id = x.Id,
                LicensePlate = x.LicensePlate,
                Capacity = x.Capacity,
                IsEnabled = x.IsEnabled
            }).ToList();

            return new PagedResult<BusDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }
    }
 }
