using BE_API.Dto.Common;
using BE_API.Dto.User;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        private const string USER_IMPORT_SUCCESS = "Import user thành công";
        private const string TEACHER_CREATE_SUCCESS = "Tạo teacher thành công";
        private const string DRIVER_CREATE_SUCCESS = "Tạo driver thành công";

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Import user từ file CSV")]
        public async Task<IActionResult> Import([FromForm] UserImportRequestDto dto, CancellationToken cancellationToken)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _userService.ImportAsync(dto, cancellationToken);
                response.Data = data;
                response.Message = USER_IMPORT_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo teacher")]
        public async Task<IActionResult> CreateTeacher([FromBody] TeacherCreateDto dto, CancellationToken cancellationToken)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _userService.CreateTeacherAsync(dto, cancellationToken);
                response.Data = data;
                response.Message = TEACHER_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo driver")]
        public async Task<IActionResult> CreateDriver([FromBody] DriverCreateDto dto, CancellationToken cancellationToken)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _userService.CreateDriverAsync(dto, cancellationToken);
                response.Data = data;
                response.Message = DRIVER_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }
    }
}
