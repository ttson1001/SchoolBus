using BE_API.Dto.Attendance;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IAttendanceService
    {
        Task<PagedResult<AttendanceDto>> SearchAttendanceAsync(string? keyword, DateTime? date, int page, int pageSize);
        Task<AttendanceDto> GetAttendanceByIdAsync(long id);
        Task<AttendanceDto> ManualCheckInAsync(AttendanceManualDto dto);
        Task<AttendanceDto> ManualCheckOutAsync(AttendanceManualDto dto);
        Task DeleteAttendanceAsync(long id);
    }
}
