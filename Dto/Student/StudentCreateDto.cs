using System.ComponentModel.DataAnnotations;

namespace BE_API.Dto.Student
{
    public class StudentCreateDto
    {
        [Required(ErrorMessage = "StudentCode khAng Aac Aa trang")]
        [MaxLength(50, ErrorMessage = "StudentCode khAng Aac vat quA 50 kA ta")]
        public string StudentCode { get; set; } = null!;

        [Required(ErrorMessage = "FullName khAng Aac Aa trang")]
        [MaxLength(100, ErrorMessage = "FullName khAng Aac vat quA 100 kA ta")]
        public string FullName { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "AvatarUrl khAng Aac vat quA 1000 kA ta")]
        public string? AvatarUrl { get; set; }

        [Required(ErrorMessage = "DateOfBirth khAng Aac Aa trang")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "GuardianId phai lan hn 0")]
        public long GuardianId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "CampusId phai lan hn 0")]
        public long CampusId { get; set; }
    }
}
