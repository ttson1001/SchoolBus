using BE_API.Dto.Booking;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        private const string BOOKING_GET_SUCCESS = "Lấy booking thành công";
        private const string BOOKING_LIST_SUCCESS = "Lấy danh sách booking thành công";
        private const string BOOKING_CREATE_SUCCESS = "Tạo booking thành công";
        private const string BOOKING_UPDATE_SUCCESS = "Cập nhật booking thành công";
        private const string BOOKING_DELETE_SUCCESS = "Xóa booking thành công";
        private const string BOOKING_AUTO_ASSIGN_SUCCESS = "Chia học sinh vào xe thành công";
        private const string BOOKING_AUTO_ASSIGN_BY_DATE_SUCCESS = "Chia học sinh vào xe theo ngày thành công";
        private const string BUS_RUN_LIST_SUCCESS = "Lấy danh sách lịch chạy thực tế thành công";
        private const string GUARDIAN_TODAY_BUS_RUN_LIST_SUCCESS = "Lấy danh sách con đi xe trong ngày thành công";
        private const string BUS_RUN_ASSIGN_STAFF_SUCCESS = "Gán tài xế và giáo viên cho chuyến xe thành công";
        private const string BOOKING_WEEKLY_SLOTS_SUCCESS = "Lấy khung giờ booking theo tuần thành công";

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>Ta hAm nay Aan 7 ngAy sau (8 ngAy), khung gia theo appsettings BookingSlots a khAng query.</summary>
        [HttpGet("[action]")]
        public async Task<IActionResult> WeeklySlots()
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.GetWeeklyBookingSlotsAsync();
                response.Data = data;
                response.Message = BOOKING_WEEKLY_SLOTS_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search(
            long? studentId,
            long? routeId,
            DateTime? serviceDate,
            string? status,
            int page = 1,
            int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.SearchAsync(studentId, routeId, serviceDate, status, page, pageSize);
                response.Data = data;
                response.Message = BOOKING_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetBusRuns(
            DateTime serviceDate,
            long? routeId,
            long? busId,
            long? driverId,
            long? teacherId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.GetBusRunsAsync(serviceDate, routeId, busId, driverId, teacherId);
                response.Data = data;
                response.Message = BUS_RUN_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{guardianId}")]
        public async Task<IActionResult> GetTodayBusRunsByGuardian(long guardianId, DateTime? serviceDate)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.GetTodayBusRunsByGuardianAsync(guardianId, serviceDate);
                response.Data = data;
                response.Message = GUARDIAN_TODAY_BUS_RUN_LIST_SUCCESS;
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
                var data = await _bookingService.GetByIdAsync(id);
                response.Data = data;
                response.Message = BOOKING_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] BookingCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.CreateAsync(dto);
                response.Data = data;
                response.Message = BOOKING_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] BookingUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.UpdateAsync(id, dto);
                response.Data = data;
                response.Message = BOOKING_UPDATE_SUCCESS;
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
                await _bookingService.DeleteAsync(id);
                response.Message = BOOKING_DELETE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{busRunId}")]
        public async Task<IActionResult> AssignBusRunStaff(long busRunId, [FromBody] BusRunAssignStaffDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.AssignBusRunStaffAsync(busRunId, dto);
                response.Data = data;
                response.Message = BUS_RUN_ASSIGN_STAFF_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AutoAssignBusRuns([FromBody] AutoAssignBookingRequestDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.AutoAssignBusRunsAsync(dto);
                response.Data = data;
                response.Message = BOOKING_AUTO_ASSIGN_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AutoAssignBusRunsByDate(DateTime serviceDate)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.AutoAssignBusRunsByDateAsync(serviceDate);
                response.Data = data;
                response.Message = BOOKING_AUTO_ASSIGN_BY_DATE_SUCCESS;
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
