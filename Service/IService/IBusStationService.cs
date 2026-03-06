using BE_API.Dto.BusStation;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IBusStationService
    {
        Task<PagedResult<BusStationDto>> SearchBusStationAsync(string? keyword, int page, int pageSize);
        Task<BusStationDto> GetBusStationByIdAsync(long id);
        Task CreateBusStationAsync(BusStationCreateDto dto);
        Task<BusStationDto> UpdateBusStationAsync(long id, BusStationUpdateDto dto);
        Task DeleteBusStationAsync(long id);
    }
}
