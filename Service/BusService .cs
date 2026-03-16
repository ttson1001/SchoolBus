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
        private readonly IRepository<BusAssignment> _busAssignmentRepo;
        private readonly IRepository<Campus> _campusRepo;

        public BusService(
            IRepository<Bus> busRepo,
            IRepository<BusAssignment> busAssignmentRepo,
            IRepository<Campus> campusRepo)
        {
            _busRepo = busRepo;
            _busAssignmentRepo = busAssignmentRepo;
            _campusRepo = campusRepo;
        }

        public async Task CreateBusAsync(BusCreateDto dto)
        {
            var licensePlate = ValidateLicensePlate(dto.LicensePlate);
            var capacity = ValidateCapacity(dto.Capacity);
            var status = NormalizeStatus(dto.Status);
            var busNumber = NormalizeOptional(dto.BusNumber);
            var imageUrl = NormalizeOptional(dto.ImageUrl);
            var color = NormalizeOptional(dto.Color);
            var busType = NormalizeOptional(dto.BusType);

            await EnsureLicensePlateUniqueAsync(licensePlate);
            await EnsureBusNumberUniqueAsync(busNumber);

            var bus = new Bus
            {
                LicensePlate = licensePlate,
                Capacity = capacity,
                Status = status,
                BusNumber = busNumber,
                ImageUrl = imageUrl,
                Color = color,
                BusType = busType
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

        public async Task<List<BusDto>> GetBusesByCampusIdAsync(long campusId)
        {
            await ValidateCampusAsync(campusId);

            var buses = await _busAssignmentRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.Bus)
                .Where(x => x.Route.CampusId == campusId)
                .Select(x => x.Bus)
                .Distinct()
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return buses.Select(MapToDto).ToList();
        }

        public async Task<BusDto> UpdateBusAsync(long id, BusUpdateDto dto)
        {
            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Bus không tồn tại");

            if (dto.LicensePlate != null)
            {
                var licensePlate = ValidateLicensePlate(dto.LicensePlate);
                await EnsureLicensePlateUniqueAsync(licensePlate, id);
                bus.LicensePlate = licensePlate;
            }

            if (dto.Capacity.HasValue)
                bus.Capacity = ValidateCapacity(dto.Capacity.Value);

            if (dto.Status != null)
                bus.Status = NormalizeStatus(dto.Status);

            if (dto.BusNumber != null)
            {
                var busNumber = NormalizeOptional(dto.BusNumber);
                await EnsureBusNumberUniqueAsync(busNumber, id);
                bus.BusNumber = busNumber;
            }

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
                    x.Status.ToLower().Contains(keyword) ||
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

            return new PagedResult<BusDto>
            {
                Items = buses.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task EnsureLicensePlateUniqueAsync(string licensePlate, long? excludeId = null)
        {
            var exists = await _busRepo.Get()
                .AnyAsync(x => x.LicensePlate.ToLower() == licensePlate.ToLower() && (!excludeId.HasValue || x.Id != excludeId.Value));

            if (exists)
                throw new Exception("Biển số xe đã tồn tại");
        }

        private async Task EnsureBusNumberUniqueAsync(string? busNumber, long? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(busNumber))
                return;

            var exists = await _busRepo.Get()
                .AnyAsync(x => x.BusNumber != null && x.BusNumber.ToLower() == busNumber.ToLower() && (!excludeId.HasValue || x.Id != excludeId.Value));

            if (exists)
                throw new Exception("Số xe đã tồn tại");
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

        private static string ValidateLicensePlate(string? licensePlate)
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                throw new Exception("Biển số xe không được để trống");

            licensePlate = licensePlate.Trim();

            if (licensePlate.Length < 5 || licensePlate.Length > 20)
                throw new Exception("Biển số xe không hợp lệ");

            return licensePlate;
        }

        private static int ValidateCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new Exception("Sức chứa phải lớn hơn 0");

            if (capacity > 100)
                throw new Exception("Sức chứa không hợp lệ");

            return capacity;
        }

        private static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "ACTIVE";

            var normalized = status.Trim().ToUpper();

            if (normalized != "ACTIVE" && normalized != "DEACTIVE" && normalized != "MAINTENANCE")
                throw new Exception("Status bus không hợp lệ");

            return normalized;
        }

        private static BusDto MapToDto(Bus bus)
        {
            return new BusDto
            {
                Id = bus.Id,
                LicensePlate = bus.LicensePlate,
                Capacity = bus.Capacity,
                Status = bus.Status,
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
