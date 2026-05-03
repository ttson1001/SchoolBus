namespace BE_API.Entites
{
    public class Booking : IEntity
    {
        public long Id { get; set; }

        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public long RouteId { get; set; }
        public BusRoute Route { get; set; } = null!;

        public DateTime ServiceDate { get; set; }
        public TimeSpan StartTime { get; set; }

        public long StationId { get; set; }
        public BusStation Station { get; set; } = null!;
        public string? PickupAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? OriginalPickupAddress { get; set; }
        public double? OriginalLatitude { get; set; }
        public double? OriginalLongitude { get; set; }

        public string Status { get; set; } = null!;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
