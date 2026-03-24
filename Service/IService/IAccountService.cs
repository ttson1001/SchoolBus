using BE_API.Dto.Account;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IAccountService
    {
        Task<LoginReponseDto> LoginAsync(LoginDto dto);
        Task SendEmailAsync(SendEmailRequest request);
    }
}
