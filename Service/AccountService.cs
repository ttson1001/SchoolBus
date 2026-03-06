using BE_API.Dto.Account;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class AccountService : IAccountService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IJwtService _jwtService;

        public AccountService(
            IJwtService jwtService,
            IRepository<User> userRepo,
            IRepository<Role> roleRepo)
        {
            _roleRepo = roleRepo;
            _userRepo = userRepo;
            _jwtService = jwtService;
            
        }

        private string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        public async Task<LoginReponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new KeyNotFoundException("Không tìm thấy tài khoản");

            if (user.Status == AccountStatus.DISABLED)
                throw new Exception("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new Exception("");

            var token = _jwtService.GenerateToken(user, null);

            return new LoginReponseDto
            {
                Token = token
            };
        }
    }
}
