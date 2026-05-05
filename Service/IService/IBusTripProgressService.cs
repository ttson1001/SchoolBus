using BE_API.Dto.BusTripProgress;

namespace BE_API.Service.IService
{
    public interface IBusTripProgressService
    {
        Task<BusTripProgressEventDto> MarkArrivedAsync(BusTripProgressArriveDto dto);
        Task<BusTripProgressCurrentDto> GetCurrentAsync(long busId, long busRunId, DateTime? rideDate);
        Task<List<BusTripProgressDriverScheduleDto>> GetDriverSchedulesAsync(long? driverId, DateTime? rideDate, TimeSpan? atTime);
        Task<List<BusTripProgressDriverScheduleDto>> GetTeacherSchedulesAsync(long? teacherId, DateTime? rideDate, TimeSpan? atTime);
        Task<List<BusTripProgressHistoryDto>> GetHistoryAsync(long? busId, long? routeId, long? campusId, DateTime? fromDate, DateTime? toDate);
    }
}
