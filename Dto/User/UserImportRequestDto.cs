using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.User
{
    public class UserImportRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
