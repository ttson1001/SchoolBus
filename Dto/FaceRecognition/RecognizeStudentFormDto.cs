using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.FaceRecognition
{
    public class RecognizeStudentFormDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
