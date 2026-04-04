using BE_API.Dto.Common;
using BE_API.Dto.Upload;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;

        private const string UPLOAD_SUCCESS = "Upload ảnh thành công";

        public UploadController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("Image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageFormDto dto)
        {
            var response = new ResponseDto();

            try
            {
                response.Data = await _cloudinaryService.UploadImageAsync(dto.File);
                response.Message = UPLOAD_SUCCESS;
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
