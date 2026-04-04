using BE_API.Dto.Common;
using BE_API.Dto.TransactionLog;

namespace BE_API.Service.IService
{
    public interface ITransactionLogService
    {
        Task<PagedResult<TransactionLogDto>> SearchAsync(string? keyword, string? method, string? status, long? orderId, string? code, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
        Task<TransactionLogDto> GetByIdAsync(long id);
        Task<TransactionLogDto> CreateAsync(TransactionLogCreateDto dto);
        Task<TransactionLogDto> UpdateAsync(long id, TransactionLogUpdateDto dto);
        Task DeleteAsync(long id);
    }
}
