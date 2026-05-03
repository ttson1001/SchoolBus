namespace BE_API.Dto.Booking
{
    public class BookingDto
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public long GuardianId { get; set; }
        public string GuardianName { get; set; } = null!;
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public DateTime ServiceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public long StationId { get; set; }
        public string StationName { get; set; } = null!;
        public string? StationAddress { get; set; }
        public string? PickupAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? OriginalPickupAddress { get; set; }
        public double? OriginalLatitude { get; set; }
        public double? OriginalLongitude { get; set; }
        public string Status { get; set; } = null!;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
