namespace BE_API.Dto.Booking
{
    public class BookingUpdateDto
    {
        public long? StudentId { get; set; }
        public long? RouteId { get; set; }
        public DateTime? ServiceDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public long? StationId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
    }
}
