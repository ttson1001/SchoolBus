using BE_API.Dto.User;
using System.Text.Json.Nodes;

namespace BE_API.Dto.Student
{
    public class StudentDetailDto
    {
        public long Id { get; set; }
        public string StudentCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public long GuardianId { get; set; }
        public UserDto Guardian { get; set; } = null!;
        public long CampusId { get; set; }
        public string CampusName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public JsonNode? FaceAiRegisteredFaces { get; set; }
    }
}
