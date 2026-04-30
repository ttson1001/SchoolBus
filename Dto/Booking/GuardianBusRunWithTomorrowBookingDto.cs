namespace BE_API.Dto.Booking
{
    public class GuardianBusRunWithTomorrowBookingDto
    {
        public GuardianTodayBusRunDto TodayBusRun { get; set; } = null!;
        public BookingDto? BookingTomorrow { get; set; }
    }
}
