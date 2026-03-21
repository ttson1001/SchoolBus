namespace BE_API.Dto.BusSchedule
{
    public class BusScheduleDto
    {
        public long Id { get; set; }
        public long BusId { get; set; }
        public string BusLabel { get; set; } = null!;
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public long CampusId { get; set; }
        public string CampusName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DayOfWeek { get; set; }
        public string ShiftType { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
