namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressHistoryDto
    {
        public long BusScheduleId { get; set; }
        public long BusId { get; set; }
        public string BusLabel { get; set; } = null!;
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public long CampusId { get; set; }
        public string CampusName { get; set; } = null!;
        public DateTime RideDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string ShiftType { get; set; } = null!;
        public long? DriverId { get; set; }
        public string? DriverName { get; set; }
        public long? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int PlannedStudentCount { get; set; }
        public int ActualStudentCount { get; set; }
        public int VisitedStationCount { get; set; }
        public int TotalStationCount { get; set; }
        public DateTime? ActualStartAt { get; set; }
        public DateTime? ActualEndAt { get; set; }
        public bool IsCompleted { get; set; }
        public string TripStatus { get; set; } = null!;
        public List<BusTripProgressHistoryStudentDto> Students { get; set; } = new();
    }
}
