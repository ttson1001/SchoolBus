using BE_API.Dto.Account;

namespace BE_API.Service
{
    public interface IAccountService
    {
        Task<LoginReponseDto> LoginAsync(LoginDto dto);
    }
}
