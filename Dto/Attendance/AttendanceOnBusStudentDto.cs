namespace BE_API.Dto.Attendance
{
    public class AttendanceOnBusStudentDto
    {
        public long AttendanceId { get; set; }
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public long GuardianId { get; set; }
        public string GuardianName { get; set; } = null!;
        public long BusId { get; set; }
        public string BusLicensePlate { get; set; } = null!;
        public DateTime Date { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public long? CheckInStationId { get; set; }
        public string? CheckInStationName { get; set; }
        public string? CheckInImageUrl { get; set; }
        public string? Note { get; set; }
        public string Method { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
