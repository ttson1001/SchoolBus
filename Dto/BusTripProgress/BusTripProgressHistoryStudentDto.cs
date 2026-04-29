namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressHistoryStudentDto
    {
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public long? StationId { get; set; }
        public string? StationName { get; set; }
        public string AssignmentType { get; set; } = null!;
    }
}
