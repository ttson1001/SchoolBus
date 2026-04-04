using BE_API.Dto.BusAssignment;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusAssignmentController : ControllerBase
    {
        private readonly IBusAssignmentService _busAssignmentService;

        private const string BUS_ASSIGNMENT_GET_SUCCESS = "Lấy bus assignment thành công";
        private const string BUS_ASSIGNMENT_LIST_SUCCESS = "Lấy danh sách bus assignment thành công";
        private const string BUS_ASSIGNMENT_CREATE_SUCCESS = "Tạo bus assignment thành công";
        private const string BUS_ASSIGNMENT_UPDATE_SUCCESS = "Cập nhật bus assignment thành công";
        private const string BUS_ASSIGNMENT_DELETE_SUCCESS = "Xóa bus assignment thành công";

        public BusAssignmentController(IBusAssignmentService busAssignmentService)
        {
            _busAssignmentService = busAssignmentService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search(
            string? keyword,
            long? busScheduleId,
            long? driverId,
            long? teacherId,
            DateTime? activeDate,
            int page = 1,
            int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busAssignmentService.SearchAsync(keyword, busScheduleId, driverId, teacherId, activeDate, page, pageSize);
                response.Data = data;
                response.Message = BUS_ASSIGNMENT_LIST_SUCCESS;
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
                var data = await _busAssignmentService.GetByIdAsync(id);
                response.Data = data;
                response.Message = BUS_ASSIGNMENT_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] BusAssignmentCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busAssignmentService.CreateAsync(dto);
                response.Data = data;
                response.Message = BUS_ASSIGNMENT_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] BusAssignmentUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busAssignmentService.UpdateAsync(id, dto);
                response.Data = data;
                response.Message = BUS_ASSIGNMENT_UPDATE_SUCCESS;
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
                await _busAssignmentService.DeleteAsync(id);
                response.Message = BUS_ASSIGNMENT_DELETE_SUCCESS;
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
