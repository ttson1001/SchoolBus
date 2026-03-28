using BE_API.Dto.Account;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Đăng nhập tài khoản", Description = "Trả về JWT token khi đăng nhập thành công. Mobile có thể truyền thêm deviceToken để lưu Firebase token, còn web có thể bỏ qua.")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _accountService.LoginAsync(dto);
                return Ok(new ResponseDto { Data = data, Message = "Đăng nhập thành công." });
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [Authorize]
        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy thông tin tài khoản hiện tại", Description = "Lấy profile user hiện tại trực tiếp từ token.")]
        public async Task<IActionResult> Me()
        {
            var response = new ResponseDto();

            try
            {
                var userId = GetCurrentUserId();
                var data = await _accountService.GetMeAsync(userId);
                return Ok(new ResponseDto { Data = data, Message = "Lấy thông tin tài khoản thành công." });
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Gửi email", Description = "Gửi email bằng cấu hình SMTP trong appsettings.")]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        {
            var response = new ResponseDto();

            try
            {
                await _accountService.SendEmailAsync(request);
                return Ok(new ResponseDto { Message = "Gửi email thành công." });
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        private long GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue("UserId");

            if (string.IsNullOrWhiteSpace(userIdValue) || !long.TryParse(userIdValue, out var userId))
                throw new Exception("Không đọc được UserId từ token");

            return userId;
        }
    }
}
