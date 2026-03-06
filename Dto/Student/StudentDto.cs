namespace BE_API.Dto.Student
{
    public class StudentDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        public long GuardianId { get; set; }
        public string GuardianName { get; set; } = null!;

        public long CampusId { get; set; }
        public string CampusName { get; set; } = null!;

        public string Status { get; set; } = null!;
    }
}
