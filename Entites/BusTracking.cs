namespace BE_API.Entites
{
    public class BusTracking : IEntity
    {
        public long Id { get; set; }

        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime TrackedAt { get; set; } = DateTime.UtcNow;
    }
}
