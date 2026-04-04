namespace BE_API.Dto.Attendance
{
    public class AttendanceDto
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public long BusId { get; set; }
        public string BusLicensePlate { get; set; } = null!;
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public long? CheckInStationId { get; set; }
        public string? CheckInStationName { get; set; }
        public string? CheckInImageUrl { get; set; }
        public long? CheckOutStationId { get; set; }
        public string? CheckOutStationName { get; set; }
        public string? CheckOutImageUrl { get; set; }
        public string? Note { get; set; }
        public string Method { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
