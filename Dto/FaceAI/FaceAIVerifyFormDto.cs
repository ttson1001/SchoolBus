using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.FaceAI
{
    public class FaceAIVerifyFormDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
