using BE_API.Dto.Common;
using BE_API.Dto.FaceRecognition;
using BE_API.Dto.SystemSetting;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingController : ControllerBase
    {
        private readonly ISystemSettingService _systemSettingService;

        private const string GET_SIMILARITY_THRESHOLD_SUCCESS = "Lay SimilarityThreshold thanh cong";
        private const string UPDATE_SIMILARITY_THRESHOLD_SUCCESS = "Cap nhat SimilarityThreshold thanh cong";
        private const string GET_BOOKING_DISTANCE_SUCCESS = "Lay BookingPickupDistanceMeters thanh cong";
        private const string UPDATE_BOOKING_DISTANCE_SUCCESS = "Cap nhat BookingPickupDistanceMeters thanh cong";

        public SettingController(ISystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lay SimilarityThreshold hien tai tu database")]
        public async Task<IActionResult> GetSimilarityThreshold()
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await _systemSettingService.GetSimilarityThresholdAsync();
                response.Message = GET_SIMILARITY_THRESHOLD_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]")]
        [SwaggerOperation(Summary = "Cap nhat SimilarityThreshold dong va luu vao database")]
        public async Task<IActionResult> UpdateSimilarityThreshold([FromBody] UpdateSimilarityThresholdDto dto)
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await _systemSettingService.UpdateSimilarityThresholdAsync(dto.SimilarityThreshold);
                response.Message = UPDATE_SIMILARITY_THRESHOLD_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lay gioi han khoang cach booking toi bus station tu database")]
        public async Task<IActionResult> GetBookingPickupDistanceMeters()
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await _systemSettingService.GetBookingPickupDistanceMetersAsync();
                response.Message = GET_BOOKING_DISTANCE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]")]
        [SwaggerOperation(Summary = "Cap nhat gioi han khoang cach booking toi bus station va luu vao database")]
        public async Task<IActionResult> UpdateBookingPickupDistanceMeters([FromBody] UpdateBookingPickupDistanceSettingDto dto)
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await _systemSettingService.UpdateBookingPickupDistanceMetersAsync(dto.BookingPickupDistanceMeters);
                response.Message = UPDATE_BOOKING_DISTANCE_SUCCESS;
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
