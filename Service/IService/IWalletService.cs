using BE_API.Dto.Wallet;
using PayOS.Models;
using PayOS.Models.Webhooks;

namespace BE_API.Service.IService
{
    public interface IWalletService
    {
        Task<WalletDto> TopUpAsync(WalletTopUpDto dto);
        Task<WalletDto> GetWalletByUserIdAsync(long userId);
        Task<WalletPayOsLinkDto> CreatePayOsTopUpLinkAsync(WalletPayOsCreateDto dto);
        Task<WalletPayOsWebhookResultDto> HandlePayOsWebhookAsync(Webhook webhook);
        Task<WalletPayOsWebhookResultDto> GetPayOsTopUpStatusAsync(long orderCode);
    }
}
