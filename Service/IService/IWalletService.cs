using BE_API.Dto.Common;
using BE_API.Dto.Wallet;
using PayOS.Models;
using PayOS.Models.Webhooks;

namespace BE_API.Service.IService
{
    public interface IWalletService
    {
        Task<PagedResult<WalletDto>> SearchAsync(string? keyword, int page, int pageSize);
        Task<WalletDto> TopUpAsync(WalletTopUpDto dto);
        Task<WalletDto> GetWalletByUserIdAsync(long userId);
        Task<PagedResult<WalletTransactionHistoryDto>> GetTransactionHistoryAsync(long walletId, DateTime? fromDate, DateTime? toDate, string? method, string? status, int page, int pageSize);
        Task<WalletPayOsLinkDto> CreatePayOsTopUpLinkAsync(WalletPayOsCreateDto dto);
        Task<WalletPayOsWebhookResultDto> HandlePayOsWebhookAsync(Webhook webhook);
        Task<WalletPayOsWebhookResultDto> GetPayOsTopUpStatusAsync(long orderCode);
    }
}
