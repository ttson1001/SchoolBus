using BE_API.Dto.BusTripProgress;

namespace BE_API.Service.IService
{
    public interface IBusTripProgressService
    {
        Task<BusTripProgressEventDto> MarkArrivedAsync(BusTripProgressArriveDto dto);
        Task<BusTripProgressCurrentDto> GetCurrentAsync(long busId, long busScheduleId, DateTime? rideDate);
        Task<List<BusTripProgressDriverScheduleDto>> GetDriverSchedulesAsync(long driverId, DateTime? rideDate, TimeSpan? atTime);
    }
}
