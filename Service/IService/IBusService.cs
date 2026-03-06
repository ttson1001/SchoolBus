using BE_API.Dto.Bus;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IBusService
    {
        Task CreateBusAsync(BusCreateDto dto);
        Task<BusDto> GetBusByIdAsync(long id);
        Task<BusDto> UpdateBusAsync(long id, BusUpdateDto dto);
        Task DeleteBusAsync(long id);
        Task<PagedResult<BusDto>> SearchBusAsync(string? keyword, int page, int pageSize);
    }
}
