using BE_API.Dto.Common;
using BE_API.Dto.FaceAI;
using BE_API.Dto.FaceRecognition;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaceAIController : ControllerBase
    {
        private readonly IFaceAIService _faceAIService;

        public FaceAIController(IFaceAIService faceAIService)
        {
            _faceAIService = faceAIService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Health()
        {
            return await ExecuteAsync(() => _faceAIService.HealthAsync(), "Gọi health check FaceAI thành công");
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateStudent([FromBody] FaceAICreateStudentDto dto)
        {
            return await ExecuteAsync(() => _faceAIService.CreateStudentAsync(dto.StudentId, dto.Name), "Tạo student trên FaceAI thành công");
        }

        [HttpPost("[action]/{studentId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddStudentFace(int studentId, [FromForm] FaceAIAddFaceFormDto dto)
        {
            return await ExecuteAsync(() => _faceAIService.AddStudentFaceAsync(studentId, dto.File), "Đăng ký khuôn mặt học sinh thành công");
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetStudents()
        {
            return await ExecuteAsync(() => _faceAIService.GetStudentsAsync(), "Lấy danh sách student từ FaceAI thành công");
        }

        [HttpGet("[action]/{studentId}")]
        public async Task<IActionResult> GetStudentImages(int studentId)
        {
            return await ExecuteAsync(() => _faceAIService.GetStudentImagesAsync(studentId), "Lấy ảnh của student thành công");
        }

        [HttpGet("[action]/{studentId}")]
        public async Task<IActionResult> GetStudentFaces(int studentId)
        {
            return await ExecuteAsync(() => _faceAIService.GetStudentFacesAsync(studentId), "Lấy face metadata của student thành công");
        }

        [HttpDelete("[action]/{studentId}")]
        public async Task<IActionResult> DeleteStudent(int studentId)
        {
            return await ExecuteAsync(() => _faceAIService.DeleteStudentAsync(studentId), "Xóa student trên FaceAI thành công");
        }

        [HttpDelete("[action]/{faceId}")]
        public async Task<IActionResult> DeleteFace(int faceId)
        {
            return await ExecuteAsync(() => _faceAIService.DeleteFaceAsync(faceId), "Xóa face trên FaceAI thành công");
        }

        [HttpPost("[action]/{studentId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> VerifyStudent(int studentId, [FromForm] FaceAIVerifyFormDto dto)
        {
            return await ExecuteAsync(() => _faceAIService.VerifyStudentAsync(studentId, dto.File), "Verify student thành công");
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Verify([FromForm] FaceAIVerifyFormDto dto)
        {
            return await ExecuteAsync(() => _faceAIService.VerifyAsync(dto.File), "Verify FaceAI thành công");
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> VerifyTop([FromForm] FaceAIVerifyTopFormDto dto)
        {
            return await ExecuteAsync(() => _faceAIService.VerifyTopAsync(dto.File, dto.TopK), "Verify top FaceAI thành công");
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RecognizeCheckIn([FromForm] FaceRecognitionAttendanceFormDto dto)
        {
            return await ExecuteAsync(() => _faceAIService.RecognizeCheckInAsync(dto), "Nhận diện khuôn mặt thành công");
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RecognizeCheckOut([FromForm] FaceRecognitionAttendanceFormDto dto)
        {
            return await ExecuteAsync(() => _faceAIService.RecognizeCheckOutAsync(dto), "Nhận diện khuôn mặt thành công");
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
