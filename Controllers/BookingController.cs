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

        private const string BOOKING_GET_SUCCESS = "Lay booking thanh cong";
        private const string BOOKING_LIST_SUCCESS = "Lay danh sach booking thanh cong";
        private const string BOOKING_CREATE_SUCCESS = "Tao booking thanh cong";
        private const string BOOKING_UPDATE_SUCCESS = "Cap nhat booking thanh cong";
        private const string BOOKING_DELETE_SUCCESS = "Xoa booking thanh cong";
        private const string BOOKING_AUTO_ASSIGN_SUCCESS = "Chia hoc sinh vao xe thanh cong";
        private const string BOOKING_AUTO_ASSIGN_BY_DATE_SUCCESS = "Chia hoc sinh vao xe theo ngay thanh cong";
        private const string BUS_RUN_LIST_SUCCESS = "Lay danh sach lich chay thuc te thanh cong";
        private const string BUS_RUN_ASSIGN_STAFF_SUCCESS = "Gan tai xe va giao vien cho chuyen xe thanh cong";
        private const string BOOKING_WEEKLY_SLOTS_SUCCESS = "Lay khung gio booking theo tuan thanh cong";

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>Từ hôm nay đến 7 ngày sau (8 ngày), khung giờ theo appsettings BookingSlots — không query.</summary>
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
