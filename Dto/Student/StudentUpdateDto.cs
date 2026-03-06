namespace BE_API.Dto.Student
{
    public class StudentUpdateDto
    {
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        public long? GuardianId { get; set; }
        public long? CampusId { get; set; }

        public string? Status { get; set; }
    }
}
