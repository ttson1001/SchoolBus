using BE_API.Dto.Booking;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public TestController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateTestBookingsForTomorrow([FromBody] CreateTestBookingsForTomorrowDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _bookingService.CreateTestBookingsForTomorrowAsync(dto);
                response.Data = data;
                response.Message = "Tạo booking test cho ngày mai thành công";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteAllTomorrowBookings()
        {
            var response = new ResponseDto();

            try
            {
                var deletedCount = await _bookingService.DeleteAllTomorrowBookingsAsync();
                response.Data = new
                {
                    deletedBookings = deletedCount,
                    serviceDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")
                };
                response.Message = "Xóa toàn bộ booking ngày mai thành công";
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
