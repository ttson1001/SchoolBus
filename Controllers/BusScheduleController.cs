using BE_API.Dto.BusSchedule;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusScheduleController : ControllerBase
    {
        private readonly IBusScheduleService _busScheduleService;

        private const string BUS_SCHEDULE_GET_SUCCESS = "Lấy bus schedule thành công";
        private const string BUS_SCHEDULE_LIST_SUCCESS = "Lấy danh sách bus schedule thành công";
        private const string BUS_SCHEDULE_CREATE_SUCCESS = "Tạo bus schedule thành công";
        private const string BUS_SCHEDULE_UPDATE_SUCCESS = "Cập nhật bus schedule thành công";
        private const string BUS_SCHEDULE_DELETE_SUCCESS = "Xóa bus schedule thành công";

        public BusScheduleController(IBusScheduleService busScheduleService)
        {
            _busScheduleService = busScheduleService;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] BusScheduleCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busScheduleService.CreateBusScheduleAsync(dto);
                response.Data = data;
                response.Message = BUS_SCHEDULE_CREATE_SUCCESS;
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
                var data = await _busScheduleService.GetBusScheduleByIdAsync(id);
                response.Data = data;
                response.Message = BUS_SCHEDULE_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search(long? busId, long? routeId, long? campusId, DateTime? fromDate, DateTime? toDate)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busScheduleService.SearchBusSchedulesAsync(busId, routeId, campusId, fromDate, toDate);
                response.Data = data;
                response.Message = BUS_SCHEDULE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{busId}")]
        public async Task<IActionResult> GetByBus(long busId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busScheduleService.GetBusSchedulesByBusIdAsync(busId);
                response.Data = data;
                response.Message = BUS_SCHEDULE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{routeId}")]
        public async Task<IActionResult> GetByRoute(long routeId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busScheduleService.GetBusSchedulesByRouteIdAsync(routeId);
                response.Data = data;
                response.Message = BUS_SCHEDULE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{campusId}")]
        public async Task<IActionResult> GetByCampus(long campusId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busScheduleService.GetBusSchedulesByCampusIdAsync(campusId);
                response.Data = data;
                response.Message = BUS_SCHEDULE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAtTime(DateTime atTime, long? campusId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busScheduleService.GetBusSchedulesAtTimeAsync(atTime, campusId);
                response.Data = data;
                response.Message = BUS_SCHEDULE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] BusScheduleUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busScheduleService.UpdateBusScheduleAsync(id, dto);
                response.Data = data;
                response.Message = BUS_SCHEDULE_UPDATE_SUCCESS;
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
                await _busScheduleService.DeleteBusScheduleAsync(id);
                response.Message = BUS_SCHEDULE_DELETE_SUCCESS;
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
