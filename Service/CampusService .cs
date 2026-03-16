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

        public async Task<CampusDto> GetCampusByIdAsync(long id)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus không tồn tại");

            return MapToDto(campus);
        }

        public async Task CreateCampusAsync(CampusCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Exception("Code không được để trống");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Tên campus không được để trống");

            if (string.IsNullOrWhiteSpace(dto.Address))
                throw new Exception("Địa chỉ không được để trống");

            var campus = new Campus
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Address = dto.Address.Trim(),
                Phone = NormalizeOptional(dto.Phone),
                IsActive = dto.IsActive ?? true,
                ImageUrl = NormalizeOptional(dto.ImageUrl),
                BusId = dto.BusId
            };

            await _campusRepo.AddAsync(campus);
            await _campusRepo.SaveChangesAsync();
        }

        public async Task<CampusDto> UpdateCampusAsync(long id, CampusUpdateDto dto)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.Code))
                campus.Code = dto.Code.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Name))
                campus.Name = dto.Name.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Address))
                campus.Address = dto.Address.Trim();

            if (dto.Phone != null)
                campus.Phone = NormalizeOptional(dto.Phone);

            if (dto.IsActive.HasValue)
                campus.IsActive = dto.IsActive.Value;

            if (dto.ImageUrl != null)
                campus.ImageUrl = NormalizeOptional(dto.ImageUrl);

            if (dto.BusId.HasValue)
                campus.BusId = dto.BusId.Value;

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
                ImageUrl = campus.ImageUrl,
                BusId = campus.BusId
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
