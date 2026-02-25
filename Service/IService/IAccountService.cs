using BE_API.Dto.Account;

namespace BE_API.Service.IService
{
    public interface IAccountService
    {
        Task<LoginReponseDto> LoginAsync(LoginDto dto);
    }
}
