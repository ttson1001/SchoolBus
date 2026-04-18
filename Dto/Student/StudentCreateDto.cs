using System.ComponentModel.DataAnnotations;

namespace BE_API.Dto.Student
{
    public class StudentCreateDto
    {
        [Required(ErrorMessage = "StudentCode không được để trống")]
        [MaxLength(50, ErrorMessage = "StudentCode không được vượt quá 50 ký tự")]
        public string StudentCode { get; set; } = null!;

        [Required(ErrorMessage = "FullName không được để trống")]
        [MaxLength(100, ErrorMessage = "FullName không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "AvatarUrl không được vượt quá 1000 ký tự")]
        public string? AvatarUrl { get; set; }

        [Required(ErrorMessage = "DateOfBirth không được để trống")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "GuardianId phải lớn hơn 0")]
        public long GuardianId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "CampusId phải lớn hơn 0")]
        public long CampusId { get; set; }
    }
}
