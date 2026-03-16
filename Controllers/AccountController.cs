using BE_API.Dto.Account;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation(Summary = "Đăng nhập tài khoản", Description = "Trả về JWT token khi đăng nhập thành công.")]
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
    }
}
