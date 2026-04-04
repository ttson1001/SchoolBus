using BE_API.Dto.Attendance;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IAttendanceService
    {
        Task<PagedResult<AttendanceDto>> SearchAttendanceAsync(string? keyword, DateTime? date, long? campusId, long? busId, long? studentId, string? status, int page, int pageSize);
        Task<AttendanceDto> GetAttendanceByIdAsync(long id);
        Task<List<AttendanceDto>> GetAttendanceByStudentIdAsync(long studentId, DateTime? fromDate, DateTime? toDate);
        Task<AttendanceDto> ManualCheckInAsync(AttendanceManualDto dto);
        Task<AttendanceDto> ManualCheckOutAsync(AttendanceManualDto dto);
        Task DeleteAttendanceAsync(long id);
    }
}
