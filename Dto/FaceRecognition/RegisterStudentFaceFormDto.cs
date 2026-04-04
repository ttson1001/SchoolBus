using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.FaceRecognition
{
    public class RegisterStudentFaceFormDto
    {
        public long StudentId { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
