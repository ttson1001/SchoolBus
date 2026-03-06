using BE_API.Dto.Common;
using BE_API.Dto.Role;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class RoleService : IRoleService
    {
        private readonly IRepository<Role> _roleRepo;

        public RoleService(IRepository<Role> roleRepo)
        {
            _roleRepo = roleRepo;
        }

        public async Task<PagedResult<RoleDto>> SearchRoleAsync(string? keyword, int page, int pageSize)
        {
            var query = _roleRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();

            var roles = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = roles.Select(x => new RoleDto
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();

            return new PagedResult<RoleDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<RoleDto> GetRoleByIdAsync(long id)
        {
            var role = await _roleRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Role không tồn tại");

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name
            };
        }

        public async Task CreateRoleAsync(RoleCreateDto dto)
        {
            var role = new Role
            {
                Name = dto.Name
            };

            await _roleRepo.AddAsync(role);
        }

        public async Task<RoleDto> UpdateRoleAsync(long id, RoleUpdateDto dto)
        {
            var role = await _roleRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Role không tồn tại");

            role.Name = dto.Name;

            _roleRepo.Update(role);

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name
            };
        }

        public async Task DeleteRoleAsync(long id)
        {
            var role = await _roleRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Role không tồn tại");

            _roleRepo.Delete(role);
        }
    }
}
