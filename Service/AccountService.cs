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
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public AccountService(
            IJwtService jwtService,
            IRepository<User> userRepo,
            IRepository<Role> roleRepo,
            IOptions<EmailSettings> emailOptions,
            IFirebaseNotificationService firebaseNotificationService)
        {
            _roleRepo = roleRepo;
            _userRepo = userRepo;
            _jwtService = jwtService;
            _emailSettings = emailOptions.Value;
            _firebaseNotificationService = firebaseNotificationService;
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
                throw new Exception("Sai mật khẩu, vui lòng kiểm tra lại mật khẩu.");

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
                throw new Exception("UserId không hợp lệ");

            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("Không tìm thấy tài khoản");

            return MapToAccountDto(user);
        }

        public async Task SendEmailAsync(SendEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.To))
                throw new Exception("Email người nhận không được để trống");

            if (string.IsNullOrWhiteSpace(request.Subject))
                throw new Exception("Tiêu đề email không được để trống");

            if (string.IsNullOrWhiteSpace(request.Body))
                throw new Exception("Nội dung email không được để trống");

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

        public async Task<(bool Sent, string Detail)> SendNotificationByDeviceTokenAsync(SendNotificationByDeviceTokenDto request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceToken))
                throw new Exception("Device token không được để trống");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new Exception("Tiêu đề thông báo không được để trống");

            if (string.IsNullOrWhiteSpace(request.Body))
                throw new Exception("Nội dung thông báo không được để trống");

            return await _firebaseNotificationService.SendDiagnosticAsync(
                request.DeviceToken.Trim(),
                request.Title.Trim(),
                request.Body.Trim(),
                new Dictionary<string, string>
                {
                    ["type"] = "MANUAL_NOTIFICATION"
                });
        }

        private void ValidateEmailSettings()
        {
            if (string.IsNullOrWhiteSpace(_emailSettings.Host))
                throw new Exception("Chưa cấu hình Email:Host");

            if (_emailSettings.Port <= 0)
                throw new Exception("Chưa cấu hình Email:Port hợp lệ");

            if (string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
                throw new Exception("Chưa cấu hình Email:FromEmail");

            if (string.IsNullOrWhiteSpace(_emailSettings.Username))
                throw new Exception("Chưa cấu hình Email:Username");

            if (string.IsNullOrWhiteSpace(_emailSettings.Password))
                throw new Exception("Chưa cấu hình Email:Password");
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
                Avatar = user.AvatarUrl,
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
