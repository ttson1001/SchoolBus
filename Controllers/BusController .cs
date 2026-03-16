using BE_API.Dto.Bus;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusController : ControllerBase
    {
        private readonly IBusService _busService;

        private const string BUS_LIST_SUCCESS = "Lấy danh sách bus thành công";
        private const string BUS_GET_SUCCESS = "Lấy bus thành công";
        private const string BUS_CREATE_SUCCESS = "Tạo bus thành công";
        private const string BUS_UPDATE_SUCCESS = "Cập nhật bus thành công";
        private const string BUS_DELETE_SUCCESS = "Xóa bus thành công";

        public BusController(IBusService busService)
        {
            _busService = busService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm xe bus")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busService.SearchBusAsync(keyword, page, pageSize);
                response.Data = data;
                response.Message = BUS_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy thông tin bus theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busService.GetBusByIdAsync(id);
                response.Data = data;
                response.Message = BUS_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{campusId}")]
        [SwaggerOperation(Summary = "Lấy danh sách bus theo campus")]
        public async Task<IActionResult> GetByCampus(long campusId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busService.GetBusesByCampusIdAsync(campusId);
                response.Data = data;
                response.Message = BUS_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo bus mới")]
        public async Task<IActionResult> Create([FromBody] BusCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                await _busService.CreateBusAsync(dto);
                response.Message = BUS_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật bus")]
        public async Task<IActionResult> Update(long id, [FromBody] BusUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busService.UpdateBusAsync(id, dto);
                response.Data = data;
                response.Message = BUS_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa bus")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _busService.DeleteBusAsync(id);
                response.Message = BUS_DELETE_SUCCESS;
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
