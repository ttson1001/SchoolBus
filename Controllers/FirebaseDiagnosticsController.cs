using BE_API.Configuration;
using BE_API.Dto.Common;
using BE_API.Dto.Firebase;
using BE_API.Service.IService;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BE_API.Controllers
{
    /// <summary>
    /// Development-only helpers to verify Firebase configuration and send a test push.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FirebaseDiagnosticsController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IOptions<FirebaseSettings> _firebaseSettings;
        private readonly IFirebaseNotificationService _firebaseNotification;
        private readonly IAccountService _accountService;

        public FirebaseDiagnosticsController(
            IWebHostEnvironment environment,
            IOptions<FirebaseSettings> firebaseSettings,
            IFirebaseNotificationService firebaseNotification,
            IAccountService accountService)
        {
            _environment = environment;
            _firebaseSettings = firebaseSettings;
            _firebaseNotification = firebaseNotification;
            _accountService = accountService;
        }

        [HttpGet("[action]")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "[Dev] Trạng thái cấu hình Firebase", Description = "Chỉ hoạt động khi ASPNETCORE_ENVIRONMENT=Development. Trả về Enabled, đã init SDK, file credential có tồn tại.")]
        public IActionResult Status()
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            var settings = _firebaseSettings.Value;
            string? resolved = null;
            var exists = false;

            if (!string.IsNullOrWhiteSpace(settings.CredentialsPath))
            {
                resolved = Path.IsPathRooted(settings.CredentialsPath)
                    ? settings.CredentialsPath
                    : Path.Combine(_environment.ContentRootPath, settings.CredentialsPath);
                exists = System.IO.File.Exists(resolved);
            }

            var dto = new FirebaseStatusDto
            {
                ConfigEnabled = settings.Enabled,
                AdminSdkInitialized = FirebaseApp.DefaultInstance != null,
                ProjectId = string.IsNullOrWhiteSpace(settings.ProjectId) ? null : settings.ProjectId,
                CredentialsFileExists = exists,
                CredentialsPathResolved = resolved
            };

            return Ok(new ResponseDto { Data = dto, Message = "Firebase diagnostics (Development only)." });
        }

        [Authorize]
        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "[Dev] Gửi tin FCM thử", Description = "Chỉ Development. Cần Bearer. Body có thể truyền deviceToken; nếu không, dùng DeviceToken của user hiện tại (đã login với FCM token).")]
        public async Task<IActionResult> SendTest([FromBody] FirebaseSendTestDto? dto)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            var response = new ResponseDto();

            try
            {
                var userId = GetCurrentUserId();
                var me = await _accountService.GetMeAsync(userId);
                var token = string.IsNullOrWhiteSpace(dto?.DeviceToken)
                    ? me.DeviceToken
                    : dto!.DeviceToken!.Trim();

                if (string.IsNullOrWhiteSpace(token))
                    throw new Exception("Không có deviceToken: truyền trong body hoặc lưu token qua Login rồi gọi lại.");

                var sent = await _firebaseNotification.SendAsync(
                    token,
                    "SchoolBus — thử nghiệm FCM",
                    "Nếu bạn thấy thông báo này, tích hợp Firebase đang hoạt động.",
                    new Dictionary<string, string> { ["type"] = "TEST", ["userId"] = userId.ToString() });

                response.Data = new { sent };
                response.Message = sent
                    ? "Đã gửi tin thử qua FCM."
                    : "Không gửi được (Firebase tắt, chưa init, hoặc token rỗng). Xem GET Status và log server.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        private long GetCurrentUserId()
        {
            var userIdValue = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdValue) || !long.TryParse(userIdValue, out var userId))
                throw new Exception("Không đọc được UserId từ token");
            return userId;
        }
    }
}
