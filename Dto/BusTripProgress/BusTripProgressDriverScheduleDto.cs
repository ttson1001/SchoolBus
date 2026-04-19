namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressDriverScheduleDto
    {
        public long BusScheduleId { get; set; }
        public long BusId { get; set; }
        public string BusLabel { get; set; } = null!;
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public DateTime RideDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string ShiftType { get; set; } = null!;
        public bool IsRunningNow { get; set; }
        public bool IsUpcoming { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRecommended { get; set; }
    }
}
