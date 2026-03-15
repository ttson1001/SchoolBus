using BE_API.Dto.BusRoute;

namespace BE_API.Service.IService
{
    public interface IBusRouteService
    {
        Task<BusRouteDto> CreateBusRouteAsync(BusRouteCreateDto dto);
    }
}
