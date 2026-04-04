namespace BE_API.Dto.BusTracking
{
    public class BusTrackingDto
    {
        public long Id { get; set; }
        public long BusId { get; set; }
        public string BusLicensePlate { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public DateTime TrackedAt { get; set; }
    }
}
