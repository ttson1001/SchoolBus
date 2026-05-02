using BE_API.Dto.Attendance;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        private const string ATTENDANCE_LIST_SUCCESS = "Lấy danh sách attendance thành công";
        private const string ATTENDANCE_GET_SUCCESS = "Lấy attendance thành công";
        private const string ATTENDANCE_ON_BUS_SUCCESS = "Lấy danh sách học sinh đang trên xe thành công";
        private const string ATTENDANCE_BUS_STATUS_SUCCESS = "Lấy danh sách học sinh trên xe và chưa trên xe thành công";
        private const string ATTENDANCE_CHECKIN_SUCCESS = "Check in thủ công thành công";
        private const string ATTENDANCE_CHECKOUT_SUCCESS = "Check out thủ công thành công";
        private const string ATTENDANCE_DELETE_SUCCESS = "Xóa attendance thành công";

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm attendance")]
        public async Task<IActionResult> Search(
            string? keyword,
            DateTime? date,
            long? campusId,
            long? busId,
            long? studentId,
            long? guardianId,
            string? status,
            int page = 1,
            int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _attendanceService.SearchAttendanceAsync(keyword, date, campusId, busId, studentId, guardianId, status, page, pageSize);
                response.Data = data;
                response.Message = ATTENDANCE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy attendance theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _attendanceService.GetAttendanceByIdAsync(id);
                response.Data = data;
                response.Message = ATTENDANCE_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{studentId}")]
        [SwaggerOperation(Summary = "Lấy lịch sử attendance theo student")]
        public async Task<IActionResult> GetByStudent(long studentId, DateTime? fromDate, DateTime? toDate)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _attendanceService.GetAttendanceByStudentIdAsync(studentId, fromDate, toDate);
                response.Data = data;
                response.Message = ATTENDANCE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy danh sách học sinh đang trên xe")]
        public async Task<IActionResult> GetStudentsOnBus(long busId, DateTime? date, long? busRunId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _attendanceService.GetStudentsOnBusAsync(busId, date, busRunId);
                response.Data = data;
                response.Message = ATTENDANCE_ON_BUS_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy danh sách học sinh đã lên xe và chưa lên xe")]
        public async Task<IActionResult> GetBusStudentStatuses(long busId, DateTime? date, long? busRunId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _attendanceService.GetBusStudentStatusesAsync(busId, date, busRunId);
                response.Data = data;
                response.Message = ATTENDANCE_BUS_STATUS_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Check in thủ công")]
        public async Task<IActionResult> ManualCheckIn([FromBody] AttendanceManualDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _attendanceService.ManualCheckInAsync(dto);
                response.Data = data;
                response.Message = ATTENDANCE_CHECKIN_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Check out thủ công")]
        public async Task<IActionResult> ManualCheckOut([FromBody] AttendanceManualDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _attendanceService.ManualCheckOutAsync(dto);
                response.Data = data;
                response.Message = ATTENDANCE_CHECKOUT_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa attendance")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _attendanceService.DeleteAttendanceAsync(id);
                response.Message = ATTENDANCE_DELETE_SUCCESS;
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
