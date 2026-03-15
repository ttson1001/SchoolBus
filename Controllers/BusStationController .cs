using BE_API.Dto.BusStation;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusStationController : ControllerBase
    {
        private readonly IBusStationService _stationService;

        private const string STATION_LIST_SUCCESS = "Lấy danh sách bus station thành công";
        private const string STATION_GET_SUCCESS = "Lấy bus station thành công";
        private const string STATION_CREATE_SUCCESS = "Tạo bus station thành công";
        private const string STATION_UPDATE_SUCCESS = "Cập nhật bus station thành công";
        private const string STATION_DELETE_SUCCESS = "Xóa bus station thành công";

        public BusStationController(IBusStationService stationService)
        {
            _stationService = stationService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm bus station")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _stationService.SearchBusStationAsync(keyword, page, pageSize);
                response.Data = data;
                response.Message = STATION_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }   
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _stationService.GetBusStationByIdAsync(id);
                response.Data = data;
                response.Message = STATION_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create(BusStationCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                await _stationService.CreateBusStationAsync(dto);
                response.Message = STATION_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> Update(long id, BusStationUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _stationService.UpdateBusStationAsync(id, dto);
                response.Data = data;
                response.Message = STATION_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _stationService.DeleteBusStationAsync(id);
                response.Message = STATION_DELETE_SUCCESS;
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
