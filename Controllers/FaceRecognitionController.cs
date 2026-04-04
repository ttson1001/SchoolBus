using BE_API.Dto.Common;
using BE_API.Dto.FaceRecognition;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaceRecognitionController : ControllerBase
    {
        private readonly IFaceRecognitionService _faceRecognitionService;

        private const string CREATE_SUBJECT_SUCCESS = "Tạo subject khuôn mặt thành công";
        private const string REGISTER_FACE_SUCCESS = "Đăng ký khuôn mặt học sinh thành công";
        private const string RECOGNIZE_FACE_SUCCESS = "Nhận diện khuôn mặt thành công";

        public FaceRecognitionController(IFaceRecognitionService faceRecognitionService)
        {
            _faceRecognitionService = faceRecognitionService;
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo subject trên CompreFace")]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var subject = await _faceRecognitionService.CreateSubjectAsync(dto.Subject);
                response.Data = new { subject };
                response.Message = CREATE_SUBJECT_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Đăng ký khuôn mặt mẫu cho học sinh")]
        public async Task<IActionResult> RegisterStudentFace([FromForm] RegisterStudentFaceFormDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var subject = await _faceRecognitionService.RegisterStudentFaceAsync(dto.StudentId, dto.File);
                response.Data = new
                {
                    studentId = dto.StudentId,
                    subject
                };
                response.Message = REGISTER_FACE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Nhận diện học sinh bằng khuôn mặt")]
        public async Task<IActionResult> RecognizeStudent([FromForm] RecognizeStudentFormDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _faceRecognitionService.RecognizeStudentAsync(dto.File);
                response.Data = data;
                response.Message = RECOGNIZE_FACE_SUCCESS;
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
