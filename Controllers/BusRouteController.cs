using BE_API.Dto.BusRoute;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusRouteController : ControllerBase
    {
        private readonly IBusRouteService _busRouteService;

        private const string BUS_ROUTE_LIST_SUCCESS = "Lấy danh sách bus route thành công";
        private const string BUS_ROUTE_GET_SUCCESS = "Lấy bus route thành công";
        private const string BUS_ROUTE_CREATE_SUCCESS = "Tạo bus route thành công";
        private const string BUS_ROUTE_UPDATE_SUCCESS = "Cập nhật bus route thành công";
        private const string BUS_ROUTE_DELETE_SUCCESS = "Xóa bus route thành công";

        public BusRouteController(IBusRouteService busRouteService)
        {
            _busRouteService = busRouteService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm tuyến xe bus")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busRouteService.SearchBusRouteAsync(keyword, page, pageSize);
                response.Data = data;
                response.Message = BUS_ROUTE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy danh sách tuyến xe bus đang hoạt động")]
        public async Task<IActionResult> Active(string? keyword, long? campusId, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busRouteService.GetActiveBusRouteAsync(keyword, campusId, page, pageSize);
                response.Data = data;
                response.Message = BUS_ROUTE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy tuyến xe bus theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busRouteService.GetBusRouteByIdAsync(id);
                response.Data = data;
                response.Message = BUS_ROUTE_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo tuyến xe bus")]
        public async Task<IActionResult> Create([FromBody] BusRouteCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busRouteService.CreateBusRouteAsync(dto);
                response.Data = data;
                response.Message = BUS_ROUTE_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật tuyến xe bus")]
        public async Task<IActionResult> Update(long id, [FromBody] BusRouteUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busRouteService.UpdateBusRouteAsync(id, dto);
                response.Data = data;
                response.Message = BUS_ROUTE_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa tuyến xe bus")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _busRouteService.DeleteBusRouteAsync(id);
                response.Message = BUS_ROUTE_DELETE_SUCCESS;
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
