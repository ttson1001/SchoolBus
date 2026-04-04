using BE_API.Dto.BusRoute;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IBusRouteService
    {
        Task<PagedResult<BusRouteDto>> SearchBusRouteAsync(string? keyword, long? campusId, int page, int pageSize);
        Task<PagedResult<BusRouteDto>> GetActiveBusRouteAsync(string? keyword, long? campusId, int page, int pageSize);
        Task<BusRouteDto> GetBusRouteByIdAsync(long id);
        Task<BusRouteDto> CreateBusRouteAsync(BusRouteCreateDto dto);
        Task<BusRouteDto> UpdateBusRouteAsync(long id, BusRouteUpdateDto dto);
        Task DeleteBusRouteAsync(long id);
    }
}
