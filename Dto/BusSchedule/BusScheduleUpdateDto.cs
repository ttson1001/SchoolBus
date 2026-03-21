namespace BE_API.Dto.BusSchedule
{
    public class BusScheduleUpdateDto
    {
        public long? BusId { get; set; }
        public long? RouteId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? DayOfWeek { get; set; }
        public string? ShiftType { get; set; }
        public bool? IsActive { get; set; }
    }
}
