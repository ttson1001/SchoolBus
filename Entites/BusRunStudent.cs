namespace BE_API.Entites
{
    public class BusRunStudent : IEntity
    {
        public long Id { get; set; }
        public long BusRunId { get; set; }
        public BusRun BusRun { get; set; } = null!;
        public long BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}
