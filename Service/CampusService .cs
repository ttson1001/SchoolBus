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
                    x.Code.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();

            var campuses = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = campuses.Select(x => new CampusDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Address = x.Address,
                Phone = x.Phone,
                IsActive = x.IsActive
            }).ToList();

            return new PagedResult<CampusDto>
            {
                Items = items,
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

            return new CampusDto
            {
                Id = campus.Id,
                Code = campus.Code,
                Name = campus.Name,
                Address = campus.Address,
                Phone = campus.Phone,
                IsActive = campus.IsActive
            };
        }

        public async Task CreateCampusAsync(CampusCreateDto dto)
        {
            var campus = new Campus
            {
                Code = dto.Code,
                Name = dto.Name,
                Address = dto.Address,
                Phone = dto.Phone,
                IsActive = true
            };

            await _campusRepo.AddAsync(campus);
        }

        public async Task<CampusDto> UpdateCampusAsync(long id, CampusUpdateDto dto)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.Code))
                campus.Code = dto.Code;

            if (!string.IsNullOrWhiteSpace(dto.Name))
                campus.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                campus.Address = dto.Address;

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                campus.Phone = dto.Phone;

            if (dto.IsActive.HasValue)
                campus.IsActive = dto.IsActive.Value;

            _campusRepo.Update(campus);

            return new CampusDto
            {
                Id = campus.Id,
                Code = campus.Code,
                Name = campus.Name,
                Address = campus.Address,
                Phone = campus.Phone,
                IsActive = campus.IsActive
            };
        }

        public async Task DeleteCampusAsync(long id)
        {
            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Campus không tồn tại");

            _campusRepo.Delete(campus);
        }
    }
}
