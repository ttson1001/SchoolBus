using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.FaceAI
{
    public class FaceAIVerifyTopFormDto
    {
        public IFormFile File { get; set; } = null!;
        public int? TopK { get; set; }
    }
}
