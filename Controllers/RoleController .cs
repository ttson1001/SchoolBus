using BE_API.Dto.Common;
using BE_API.Dto.Role;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        private const string ROLE_LIST_SUCCESS = "Lấy danh sách role thành công";
        private const string ROLE_GET_SUCCESS = "Lấy role thành công";
        private const string ROLE_CREATE_SUCCESS = "Tạo role thành công";
        private const string ROLE_UPDATE_SUCCESS = "Cập nhật role thành công";
        private const string ROLE_DELETE_SUCCESS = "Xóa role thành công";

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm role")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _roleService.SearchRoleAsync(keyword, page, pageSize);
                response.Data = data;
                response.Message = ROLE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy role theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _roleService.GetRoleByIdAsync(id);
                response.Data = data;
                response.Message = ROLE_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo role")]
        public async Task<IActionResult> Create(RoleCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                await _roleService.CreateRoleAsync(dto);
                response.Message = ROLE_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật role")]
        public async Task<IActionResult> Update(long id, RoleUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _roleService.UpdateRoleAsync(id, dto);
                response.Data = data;
                response.Message = ROLE_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa role")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _roleService.DeleteRoleAsync(id);
                response.Message = ROLE_DELETE_SUCCESS;
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