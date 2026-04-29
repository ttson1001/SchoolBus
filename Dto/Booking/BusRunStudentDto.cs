namespace BE_API.Dto.Booking
{
    public class BusRunStudentDto
    {
        public long BookingId { get; set; }
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public long StationId { get; set; }
        public string StationName { get; set; } = null!;
    }
}
