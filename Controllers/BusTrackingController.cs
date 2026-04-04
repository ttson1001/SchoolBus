using BE_API.Dto.BusTracking;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusTrackingController : ControllerBase
    {
        private readonly IBusTrackingService _busTrackingService;

        private const string BUS_TRACKING_UPDATE_SUCCESS = "Cập nhật vị trí xe thành công";
        private const string BUS_TRACKING_GET_SUCCESS = "Lấy vị trí xe thành công";

        public BusTrackingController(IBusTrackingService busTrackingService)
        {
            _busTrackingService = busTrackingService;
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Cập nhật vị trí GPS của xe")]
        public async Task<IActionResult> Update([FromBody] BusTrackingUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busTrackingService.UpdateAsync(dto);
                response.Data = data;
                response.Message = BUS_TRACKING_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{busId}")]
        [SwaggerOperation(Summary = "Lấy vị trí GPS mới nhất của xe")]
        public async Task<IActionResult> GetLatest(long busId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busTrackingService.GetLatestAsync(busId);
                response.Data = data;
                response.Message = BUS_TRACKING_GET_SUCCESS;
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
