using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.Upload
{
    public class UploadImageFormDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
