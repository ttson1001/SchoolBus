namespace BE_API.Dto.Booking
{
    public class CreateTestBookingsForTomorrowDto
    {
        public long RouteId { get; set; }
        public TimeSpan StartTime { get; set; }
        public int BookingCount { get; set; } = 5;
        public long? StationId { get; set; }
        public List<long>? StudentIds { get; set; }
        public string? PickupAddressPrefix { get; set; }
    }
}
