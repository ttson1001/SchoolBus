using BE_API.Dto.Campus;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class CampusService : ICampusService
    {
        private readonly IRepository<Campus> _campusRepo;
        private readonly IRepository<BusStation> _busStationRepo;

        public CampusService(IRepository<Campus> campusRepo, IRepository<BusStation> busStationRepo)
        {
            _campusRepo = campusRepo;
            _busStationRepo = busStationRepo;
        }

        public async Task<PagedResult<CampusDto>> SearchCampusAsync(string? keyword, int page, int pageSize)
        {
            var query = BuildCampusSearchQuery(keyword);
            return await BuildCampusPagedResultAsync(query, page, pageSize);
        }

        public async Task<PagedResult<CampusDto>> GetActiveCampusesAsync(string? keyword, int page, int pageSize)
        {
            var query = BuildCampusSearchQuery(keyword)
                .Where(x => x.IsActive);

            return await BuildCampusPagedResultAsync(query, page, pageSize);
        }

        public async Task<CampusDto> GetCampusByIdAsync(long id)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus không tồn tại");

            return MapToDto(campus);
        }

        public async Task<CampusDto> CreateCampusAsync(CampusCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Exception("Code không được để trống");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Tên campus không được để trống");

            if (string.IsNullOrWhiteSpace(dto.Address))
                throw new Exception("Địa chỉ không được để trống");

            var normalizedCode = dto.Code.Trim();
            var normalizedName = dto.Name.Trim();

            await ValidateCampusDuplicateAsync(normalizedCode, normalizedName);

            var campus = new Campus
            {
                Code = normalizedCode,
                Name = normalizedName,
                Address = dto.Address.Trim(),
                Phone = NormalizePhone(dto.Phone),
                IsActive = dto.IsActive ?? true,
                ImageUrl = NormalizeOptional(dto.ImageUrl)
            };

            await _campusRepo.AddAsync(campus);
            await _campusRepo.SaveChangesAsync();

            if (dto.InitialBusStations != null && dto.InitialBusStations.Any())
            {
                var stations = new List<BusStation>();

                foreach (var stationDto in dto.InitialBusStations)
                {
                    var stationName = NormalizeOptional(stationDto.Name);
                    if (string.IsNullOrWhiteSpace(stationName))
                        throw new Exception("Tên bus station không được để trống");

                    await EnsureStationNameNotDuplicatedAsync(stationName);

                    stations.Add(new BusStation
                    {
                        CampusId = campus.Id,
                        Name = stationName,
                        Address = NormalizeOptional(stationDto.Address),
                        Description = NormalizeOptional(stationDto.Description),
                        Latitude = stationDto.Latitude,
                        Longitude = stationDto.Longitude,
                        IsEnabled = stationDto.IsEnabled ?? true
                    });
                }

                await _busStationRepo.AddRangeAsync(stations);
                await _busStationRepo.SaveChangesAsync();
            }

            return MapToDto(campus);
        }

        public async Task<CampusDto> UpdateCampusAsync(long id, CampusUpdateDto dto)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus không tồn tại");

            var nextCode = string.IsNullOrWhiteSpace(dto.Code) ? campus.Code : dto.Code.Trim();
            var nextName = string.IsNullOrWhiteSpace(dto.Name) ? campus.Name : dto.Name.Trim();

            await ValidateCampusDuplicateAsync(nextCode, nextName, id);

            if (!string.IsNullOrWhiteSpace(dto.Code))
                campus.Code = nextCode;

            if (!string.IsNullOrWhiteSpace(dto.Name))
                campus.Name = nextName;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                campus.Address = dto.Address.Trim();

            if (dto.Phone != null)
                campus.Phone = NormalizePhone(dto.Phone);

            if (dto.IsActive.HasValue)
                campus.IsActive = dto.IsActive.Value;

            if (dto.ImageUrl != null)
                campus.ImageUrl = NormalizeOptional(dto.ImageUrl);

            _campusRepo.Update(campus);
            await _campusRepo.SaveChangesAsync();

            return MapToDto(campus);
        }

        public async Task DeleteCampusAsync(long id)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus không tồn tại");

            _campusRepo.Delete(campus);
            await _campusRepo.SaveChangesAsync();
        }

        private IQueryable<Campus> BuildCampusSearchQuery(string? keyword)
        {
            var query = _campusRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(keyword) ||
                    x.Code.ToLower().Contains(keyword) ||
                    x.Address.ToLower().Contains(keyword) ||
                    (x.Phone != null && x.Phone.ToLower().Contains(keyword)));
            }

            return query;
        }

        private async Task ValidateCampusDuplicateAsync(string code, string name, long? excludeId = null)
        {
            var normalizedCode = code.Trim().ToLower();
            var normalizedName = name.Trim().ToLower();

            var duplicatedCampus = await _campusRepo.Get()
                .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
                .Where(x =>
                    x.Code.ToLower() == normalizedCode ||
                    x.Name.ToLower() == normalizedName)
                .Select(x => new { x.Code, x.Name })
                .FirstOrDefaultAsync();

            if (duplicatedCampus == null)
                return;

            if (string.Equals(duplicatedCampus.Code, code, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Code campus đã tồn tại");

            throw new Exception("Tên campus đã tồn tại");
        }

        private static async Task<PagedResult<CampusDto>> BuildCampusPagedResultAsync(IQueryable<Campus> query, int page, int pageSize)
        {
            var totalItems = await query.CountAsync();

            var campuses = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CampusDto>
            {
                Items = campuses.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        private static CampusDto MapToDto(Campus campus)
        {
            return new CampusDto
            {
                Id = campus.Id,
                Code = campus.Code,
                Name = campus.Name,
                Address = campus.Address,
                Phone = campus.Phone,
                IsActive = campus.IsActive,
                ImageUrl = campus.ImageUrl
            };
        }

        private async Task EnsureStationNameNotDuplicatedAsync(string stationName)
        {
            var normalizedStationName = stationName.Trim().ToLower();

            var duplicated = await _busStationRepo.Get()
                .AnyAsync(x => x.Name.ToLower() == normalizedStationName);

            if (duplicated)
                throw new Exception("Tên bus station đã tồn tại");
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var normalizedPhone = phone.Trim();

            if ((normalizedPhone.Length != 9 && normalizedPhone.Length != 10) || normalizedPhone.Any(x => !char.IsDigit(x)))
                throw new Exception("Số điện thoại không hợp lệ");

            return normalizedPhone;
        }
    }
}
