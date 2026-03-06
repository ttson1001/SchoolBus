using BE_API.Dto.Common;
using BE_API.Dto.Role;

namespace BE_API.Service.IService
{
    public interface IRoleService
    {
        Task<PagedResult<RoleDto>> SearchRoleAsync(string? keyword, int page, int pageSize);
        Task<RoleDto> GetRoleByIdAsync(long id);
        Task CreateRoleAsync(RoleCreateDto dto);
        Task<RoleDto> UpdateRoleAsync(long id, RoleUpdateDto dto);
        Task DeleteRoleAsync(long id);
    }
}
