using BE_API.Configuration;
using BE_API.Dto.Account;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BE_API.Service
{
    public class AccountService : IAccountService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IJwtService _jwtService;
        private readonly EmailSettings _emailSettings;

        public AccountService(
            IJwtService jwtService,
            IRepository<User> userRepo,
            IRepository<Role> roleRepo,
            IOptions<EmailSettings> emailOptions)
        {
            _roleRepo = roleRepo;
            _userRepo = userRepo;
            _jwtService = jwtService;
            _emailSettings = emailOptions.Value;
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
                ?? throw new KeyNotFoundException("Khong tim thay tai khoan");

            if (user.Status == AccountStatus.DISABLED)
                throw new Exception("Tai khoan da bi vo hieu hoa. Vui long lien he quan tri vien.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new Exception("Sai mat khau can kiem tra lai mat khau");

            var normalizedDeviceToken = NormalizeDeviceToken(dto.DeviceToken);
            if (!string.IsNullOrWhiteSpace(normalizedDeviceToken) &&
                !string.Equals(user.DeviceToken, normalizedDeviceToken, StringComparison.Ordinal))
            {
                user.DeviceToken = normalizedDeviceToken;
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();
            }

            var token = _jwtService.GenerateToken(user, null);

            return new LoginReponseDto
            {
                Token = token
            };
        }

        public async Task<AccountDto> GetMeAsync(long userId)
        {
            if (userId <= 0)
                throw new Exception("UserId khong hop le");

            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("Khong tim thay tai khoan");

            return MapToAccountDto(user);
        }

        public async Task SendEmailAsync(SendEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.To))
                throw new Exception("Email nguoi nhan khong duoc de trong");

            if (string.IsNullOrWhiteSpace(request.Subject))
                throw new Exception("Tieu de email khong duoc de trong");

            if (string.IsNullOrWhiteSpace(request.Body))
                throw new Exception("Noi dung email khong duoc de trong");

            ValidateEmailSettings();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(request.To.Trim()));
            message.Subject = request.Subject.Trim();
            message.Body = new BodyBuilder
            {
                HtmlBody = request.Body,
                TextBody = request.Body
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();
            var secureSocketOptions = _emailSettings.UseSsl
                ? SecureSocketOptions.Auto
                : SecureSocketOptions.None;
            await smtpClient.ConnectAsync(_emailSettings.Host, _emailSettings.Port, secureSocketOptions);
            await smtpClient.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);
        }

        private void ValidateEmailSettings()
        {
            if (string.IsNullOrWhiteSpace(_emailSettings.Host))
                throw new Exception("Chua cau hinh Email:Host");

            if (_emailSettings.Port <= 0)
                throw new Exception("Chua cau hinh Email:Port hop le");

            if (string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
                throw new Exception("Chua cau hinh Email:FromEmail");

            if (string.IsNullOrWhiteSpace(_emailSettings.Username))
                throw new Exception("Chua cau hinh Email:Username");

            if (string.IsNullOrWhiteSpace(_emailSettings.Password))
                throw new Exception("Chua cau hinh Email:Password");
        }

        private static AccountDto MapToAccountDto(User user)
        {
            return new AccountDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName ?? string.Empty,
                Phone = user.Phone,
                DeviceToken = user.DeviceToken,
                Avatar = null,
                RoleName = user.Role?.Name ?? string.Empty,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            };
        }

        private static string? NormalizeDeviceToken(string? deviceToken)
        {
            return string.IsNullOrWhiteSpace(deviceToken) ? null : deviceToken.Trim();
        }
    }
}
