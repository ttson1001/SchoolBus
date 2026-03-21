namespace BE_API.Entites
{
    public class BusSchedule : IEntity
    {
        public long Id { get; set; }

        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public long RouteId { get; set; }
        public BusRoute Route { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DayOfWeek { get; set; }
        public string ShiftType { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
