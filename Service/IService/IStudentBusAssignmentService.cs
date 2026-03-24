using BE_API.Dto.StudentBusAssignment;

namespace BE_API.Service.IService
{
    public interface IStudentBusAssignmentService
    {
        Task<StudentBusAssignmentDto> CreateAsync(StudentBusAssignmentCreateDto dto);
        Task<StudentBusAssignmentDto> GetByIdAsync(long id);
        Task<List<StudentBusAssignmentDto>> GetByStudentIdAsync(long studentId, DateTime? rideDate);
        Task<List<StudentBusAssignmentDto>> GetByGuardianIdAsync(long guardianId, DateTime? rideDate);
        Task<StudentBusAssignmentDto> UpdateAsync(long id, StudentBusAssignmentUpdateDto dto);
        Task DeleteAsync(long id);
    }
}
