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

        public CampusService(IRepository<Campus> campusRepo)
        {
            _campusRepo = campusRepo;
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
                ?? throw new Exception("Campus khAng tan tai");

            return MapToDto(campus);
        }

        public async Task CreateCampusAsync(CampusCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Exception("Code khAng Aac Aa trang");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("TAn campus khAng Aac Aa trang");

            if (string.IsNullOrWhiteSpace(dto.Address))
                throw new Exception("Aaa cha khAng Aac Aa trang");

            var normalizedCode = dto.Code.Trim();
            var normalizedName = dto.Name.Trim();

            await ValidateCampusDuplicateAsync(normalizedCode, normalizedName);

            var campus = new Campus
            {
                Code = normalizedCode,
                Name = normalizedName,
                Address = dto.Address.Trim(),
                Phone = NormalizeOptional(dto.Phone),
                IsActive = dto.IsActive ?? true,
                ImageUrl = NormalizeOptional(dto.ImageUrl)
            };

            await _campusRepo.AddAsync(campus);
            await _campusRepo.SaveChangesAsync();
        }

        public async Task<CampusDto> UpdateCampusAsync(long id, CampusUpdateDto dto)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus khAng tan tai");

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
                campus.Phone = NormalizeOptional(dto.Phone);

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
                ?? throw new Exception("Campus khAng tan tai");

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
                throw new Exception("Code campus AA tan tai");

            throw new Exception("TAn campus AA tan tai");
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

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
