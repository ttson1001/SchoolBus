using BE_API.Configuration;
using BE_API.Dto.Common;
using BE_API.Dto.Firebase;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    /// <summary>
    /// API test gửi push FCM công khai (không cần Bearer). Mặc định chỉ khi Development; có thể bật thêm qua Firebase:AllowPublicTestEndpoint.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FirebaseTestController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IOptions<FirebaseSettings> _firebaseSettings;
        private readonly IFirebaseNotificationService _firebaseNotification;

        public FirebaseTestController(
            IWebHostEnvironment environment,
            IOptions<FirebaseSettings> firebaseSettings,
            IFirebaseNotificationService firebaseNotification)
        {
            _environment = environment;
            _firebaseSettings = firebaseSettings;
            _firebaseNotification = firebaseNotification;
        }

        /// <summary>
        /// Gửi một thông báo thử tới token FCM (để mobile/QA kiểm tra).
        /// </summary>
        [HttpPost("[action]")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Test FCM công khai",
            Description = "Không cần Authorization. Body JSON: { \"deviceIdToken\": \"&lt;FCM registration token&gt;\" }. Hoạt động khi Environment=Development hoặc Firebase:AllowPublicTestEndpoint=true.")]
        [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Send([FromBody] FirebasePublicTestDto dto)
        {
            if (!_environment.IsDevelopment() && !_firebaseSettings.Value.AllowPublicTestEndpoint)
                return NotFound();

            var response = new ResponseDto();

            if (dto == null || string.IsNullOrWhiteSpace(dto.DeviceIdToken))
            {
                response.Message = "deviceIdToken là bắt buộc.";
                return BadRequest(response);
            }

            var token = dto.DeviceIdToken.Trim();
            var (sent, detail) = await _firebaseNotification.SendDiagnosticAsync(
                token,
                "SchoolBus — test FCM (public)",
                "Nếu bạn nhận được tin này, token và Firebase backend đang hoạt động.",
                new Dictionary<string, string>
                {
                    ["type"] = "PUBLIC_TEST"
                });

            response.Data = new { sent, detail };
            response.Message = sent
                ? "Đã gửi tin thử qua FCM."
                : detail;
            return Ok(response);
        }
    }
}
