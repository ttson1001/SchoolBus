namespace BE_API.Dto.Attendance
{
    public class AttendanceManualDto
    {
        public long StudentId { get; set; }
        public long BusId { get; set; }
        public DateTime? Date { get; set; }
        public TimeSpan? Time { get; set; }
    }
}
