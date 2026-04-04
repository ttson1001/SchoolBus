using BE_API.Dto.Common;
using BE_API.Dto.StudentBusAssignment;

namespace BE_API.Service.IService
{
    public interface IStudentBusAssignmentService
    {
        Task<PagedResult<StudentBusAssignmentDto>> SearchAsync(string? keyword, long? studentId, long? guardianId, long? busId, long? routeId, DateTime? rideDate, int page, int pageSize);
        Task<StudentBusAssignmentDto> CreateAsync(StudentBusAssignmentCreateDto dto);
        Task<StudentBusAssignmentDto> CreateByScheduleAsync(StudentBusAssignmentByScheduleCreateDto dto);
        Task<StudentBusAssignmentDto> GetByIdAsync(long id);
        Task<List<StudentBusAssignmentDto>> GetByStudentIdAsync(long studentId, DateTime? rideDate);
        Task<List<StudentBusAssignmentDto>> GetByGuardianIdAsync(long guardianId, DateTime? rideDate);
        Task<StudentBusAssignmentDto> UpdateAsync(long id, StudentBusAssignmentUpdateDto dto);
        Task<StudentBusAssignmentDto> UpdateByScheduleAsync(long id, StudentBusAssignmentByScheduleUpdateDto dto);
        Task DeleteAsync(long id);
    }
}
