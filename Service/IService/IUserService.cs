using BE_API.Dto.User;

namespace BE_API.Service.IService
{
    public interface IUserService
    {
        Task<UserImportResultDto> ImportAsync(UserImportRequestDto dto, CancellationToken cancellationToken = default);
        Task<TeacherDto> CreateTeacherAsync(TeacherCreateDto dto, CancellationToken cancellationToken = default);
        Task<DriverDto> CreateDriverAsync(DriverCreateDto dto, CancellationToken cancellationToken = default);
    }
}
