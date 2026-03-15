using BE_API.Dto.BusDamageReport;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IBusDamageReportService
    {
        Task<PagedResult<BusDamageReportDto>> SearchBusDamageReportAsync(string? keyword, string? status, int page, int pageSize);
        Task<BusDamageReportDto> GetBusDamageReportByIdAsync(long id);
        Task CreateBusDamageReportAsync(BusDamageReportCreateDto dto);
        Task<BusDamageReportDto> UpdateBusDamageReportAsync(long id, BusDamageReportUpdateDto dto);
        Task DeleteBusDamageReportAsync(long id);
    }
}
