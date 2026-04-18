using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.Student
{
    public class StudentImportByGuardianEmailRequestDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
