using BE_API.Dto.BusTracking;

namespace BE_API.Service.IService
{
    public interface IBusTrackingService
    {
        Task<BusTrackingDto> UpdateAsync(BusTrackingUpdateDto dto);
        Task<BusTrackingDto> GetLatestAsync(long busId);
    }
}
