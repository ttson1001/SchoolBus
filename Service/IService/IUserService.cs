using BE_API.Dto.Common;
using BE_API.Dto.User;

namespace BE_API.Service.IService
{
    public interface IUserService
    {
        Task<PagedResult<UserDto>> SearchUserAsync(string? keyword, string? role, string? status, int page, int pageSize);
        Task<UserDto> GetUserByIdAsync(long id);
        Task<UserImportResultDto> ImportAsync(UserImportRequestDto dto, CancellationToken cancellationToken = default);
        Task<UserDto> CreateUserAsync(UserCreateDto dto, CancellationToken cancellationToken = default);
        Task<UserDto> UpdateUserAsync(long id, UserUpdateDto dto, CancellationToken cancellationToken = default);
        Task<TeacherDto> CreateTeacherAsync(TeacherCreateDto dto, CancellationToken cancellationToken = default);
        Task<DriverDto> CreateDriverAsync(DriverCreateDto dto, CancellationToken cancellationToken = default);
    }
}
