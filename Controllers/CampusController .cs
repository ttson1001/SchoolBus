using BE_API.Dto.Campus;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampusController : ControllerBase
    {
        private readonly ICampusService _campusService;

        private const string CAMPUS_LIST_SUCCESS = "Lấy danh sách campus thành công";
        private const string CAMPUS_GET_SUCCESS = "Lấy campus thành công";
        private const string CAMPUS_CREATE_SUCCESS = "Tạo campus thành công";
        private const string CAMPUS_UPDATE_SUCCESS = "Cập nhật campus thành công";
        private const string CAMPUS_DELETE_SUCCESS = "Xóa campus thành công";

        public CampusController(ICampusService campusService)
        {
            _campusService = campusService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm campus")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _campusService.SearchCampusAsync(keyword, page, pageSize);
                response.Data = data;
                response.Message = CAMPUS_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy campus theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _campusService.GetCampusByIdAsync(id);
                response.Data = data;
                response.Message = CAMPUS_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo campus")]
        public async Task<IActionResult> Create(CampusCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                await _campusService.CreateCampusAsync(dto);
                response.Message = CAMPUS_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật campus")]
        public async Task<IActionResult> Update(long id, CampusUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _campusService.UpdateCampusAsync(id, dto);
                response.Data = data;
                response.Message = CAMPUS_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa campus")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _campusService.DeleteCampusAsync(id);
                response.Message = CAMPUS_DELETE_SUCCESS;
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
