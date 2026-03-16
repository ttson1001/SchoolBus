using BE_API.Dto.Common;
using BE_API.Dto.Student;

namespace BE_API.Service.IService
{
    public interface IStudentService
    {
        Task<PagedResult<StudentDto>> SearchStudentAsync(string? keyword, int page, int pageSize);
        Task<StudentDto> GetStudentByIdAsync(long id);
        Task<List<StudentDto>> GetStudentsByCampusIdAsync(long campusId);
        Task<List<StudentDto>> GetStudentsByGuardianIdAsync(long guardianId);
        Task CreateStudentAsync(StudentCreateDto dto);
        Task<StudentDto> UpdateStudentAsync(long id, StudentUpdateDto dto);
        Task DeleteStudentAsync(long id);
    }
}
