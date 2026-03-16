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

        private const string USER_LIST_SUCCESS = "Lấy danh sách user thành công";
        private const string USER_GET_SUCCESS = "Lấy user thành công";
        private const string USER_IMPORT_SUCCESS = "Import user thành công";
        private const string USER_CREATE_SUCCESS = "Tạo user thành công";
        private const string USER_UPDATE_SUCCESS = "Cập nhật user thành công";
        private const string TEACHER_CREATE_SUCCESS = "Tạo teacher thành công";
        private const string DRIVER_CREATE_SUCCESS = "Tạo driver thành công";

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm user")]
        public async Task<IActionResult> Search(string? keyword, string? role, string? status, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _userService.SearchUserAsync(keyword, role, status, page, pageSize);
                response.Data = data;
                response.Message = USER_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy user theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _userService.GetUserByIdAsync(id);
                response.Data = data;
                response.Message = USER_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Import user từ file Excel")]
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
        [SwaggerOperation(Summary = "Tạo user theo role truyền vào")]
        public async Task<IActionResult> Create([FromBody] UserCreateDto dto, CancellationToken cancellationToken)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _userService.CreateUserAsync(dto, cancellationToken);
                response.Data = data;
                response.Message = USER_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật user")]
        public async Task<IActionResult> Update(long id, [FromBody] UserUpdateDto dto, CancellationToken cancellationToken)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _userService.UpdateUserAsync(id, dto, cancellationToken);
                response.Data = data;
                response.Message = USER_UPDATE_SUCCESS;
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
