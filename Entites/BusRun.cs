namespace BE_API.Entites
{
    public class BusRun : IEntity
    {
        public long Id { get; set; }
        public long RouteId { get; set; }
        public BusRoute Route { get; set; } = null!;
        public DateTime ServiceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;
        public long? DriverId { get; set; }
        public User? Driver { get; set; }
        public long? TeacherId { get; set; }
        public User? Teacher { get; set; }
        public int SeatCapacity { get; set; }
        public int UsableCapacity { get; set; }
        public int AssignedStudentCount { get; set; }
        public int RunOrder { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<BusRunStudent> Students { get; set; } = new List<BusRunStudent>();
    }
}
