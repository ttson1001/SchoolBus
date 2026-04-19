using BE_API.Dto.BusTripProgress;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusTripProgressController : ControllerBase
    {
        private readonly IBusTripProgressService _busTripProgressService;

        public BusTripProgressController(IBusTripProgressService busTripProgressService)
        {
            _busTripProgressService = busTripProgressService;
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tài xế xác nhận xe đã đến trạm")]
        public async Task<IActionResult> Arrive([FromBody] BusTripProgressArriveDto dto)
        {
            return await ExecuteAsync(
                () => _busTripProgressService.MarkArrivedAsync(dto),
                "Xác nhận đến trạm thành công");
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy danh sách lịch chạy trong ngày của tài xế")]
        public async Task<IActionResult> DriverSchedules(long driverId, DateTime? rideDate, TimeSpan? atTime)
        {
            return await ExecuteAsync(
                () => _busTripProgressService.GetDriverSchedulesAsync(driverId, rideDate, atTime),
                "Lấy danh sách lịch chạy của tài xế thành công");
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy danh sách lịch chạy trong ngày của giáo viên")]
        public async Task<IActionResult> TeacherSchedules(long teacherId, DateTime? rideDate, TimeSpan? atTime)
        {
            return await ExecuteAsync(
                () => _busTripProgressService.GetTeacherSchedulesAsync(teacherId, rideDate, atTime),
                "Lấy danh sách lịch chạy của giáo viên thành công");
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy trạng thái hiện tại của chuyến xe theo lịch chạy")]
        public async Task<IActionResult> Current(long busId, long busScheduleId, DateTime? rideDate)
        {
            return await ExecuteAsync(
                () => _busTripProgressService.GetCurrentAsync(busId, busScheduleId, rideDate),
                "Lấy trạng thái chuyến xe thành công");
        }

        private static async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action, string successMessage)
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await action();
                response.Message = successMessage;
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return new BadRequestObjectResult(response);
            }
        }
    }
}
