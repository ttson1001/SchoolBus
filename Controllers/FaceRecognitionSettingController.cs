using BE_API.Dto.Common;
using BE_API.Dto.FaceRecognition;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaceRecognitionSettingController : ControllerBase
    {
        private readonly ISystemSettingService _systemSettingService;

        private const string GET_SUCCESS = "Lấy SimilarityThreshold thành công";
        private const string UPDATE_SUCCESS = "Cập nhật SimilarityThreshold thành công";

        public FaceRecognitionSettingController(ISystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Lấy SimilarityThreshold hiện tại từ database")]
        public async Task<IActionResult> GetSimilarityThreshold()
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await _systemSettingService.GetSimilarityThresholdAsync();
                response.Message = GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]")]
        [SwaggerOperation(Summary = "Cập nhật SimilarityThreshold động và lưu vào database")]
        public async Task<IActionResult> UpdateSimilarityThreshold([FromBody] UpdateSimilarityThresholdDto dto)
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await _systemSettingService.UpdateSimilarityThresholdAsync(dto.SimilarityThreshold);
                response.Message = UPDATE_SUCCESS;
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
