namespace BE_API.Dto.Booking
{
    public class BusRunDto
    {
        public long Id { get; set; }
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public DateTime ServiceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public long BusId { get; set; }
        public string BusLabel { get; set; } = null!;
        public long? DriverId { get; set; }
        public string? DriverName { get; set; }
        public long? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int SeatCapacity { get; set; }
        public int UsableCapacity { get; set; }
        public int AssignedStudentCount { get; set; }
        public int RunOrder { get; set; }
        public string Status { get; set; } = null!;
        public List<BusRunStudentDto> Students { get; set; } = new();
    }
}
