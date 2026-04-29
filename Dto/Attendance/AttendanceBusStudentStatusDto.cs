namespace BE_API.Dto.Attendance
{
    public class AttendanceBusStudentStatusDto
    {
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string? StudentAvatarUrl { get; set; }
        public long GuardianId { get; set; }
        public string GuardianName { get; set; } = null!;
        public string? GuardianPhone { get; set; }
        public long BookingId { get; set; }
        public long StationId { get; set; }
        public string StationName { get; set; } = null!;
        public long? AttendanceId { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public bool IsOnBus { get; set; }
    }
}
