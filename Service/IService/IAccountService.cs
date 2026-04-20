using BE_API.Dto.Account;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IAccountService
    {
        Task<LoginReponseDto> LoginAsync(LoginDto dto);
        Task<AccountDto> GetMeAsync(long userId);
        Task SendEmailAsync(SendEmailRequest request);
        Task<(bool Sent, string Detail)> SendNotificationByDeviceTokenAsync(SendNotificationByDeviceTokenDto request);
    }
}
