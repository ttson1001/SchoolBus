namespace BE_API.Dto.Booking
{
    public class AutoAssignBookingRequestDto
    {
        public long RouteId { get; set; }
        public DateTime ServiceDate { get; set; }
        public TimeSpan StartTime { get; set; }
    }
}
