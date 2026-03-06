using BE_API.Dto.Campus;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface ICampusService
    {
        Task<PagedResult<CampusDto>> SearchCampusAsync(string? keyword, int page, int pageSize);
        Task<CampusDto> GetCampusByIdAsync(long id);
        Task CreateCampusAsync(CampusCreateDto dto);
        Task<CampusDto> UpdateCampusAsync(long id, CampusUpdateDto dto);
        Task DeleteCampusAsync(long id);
    }
}
