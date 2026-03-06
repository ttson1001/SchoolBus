namespace BE_API.Dto.Student
{
    public class StudentCreateDto
    {
        public string FullName { get; set; } = null!;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        public long GuardianId { get; set; }
        public long CampusId { get; set; }
    }
}
