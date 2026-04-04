namespace BE_API.Dto.BusTracking
{
    public class BusTrackingUpdateDto
    {
        public long BusId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
    }
}
