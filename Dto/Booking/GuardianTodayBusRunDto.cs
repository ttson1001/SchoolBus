namespace BE_API.Dto.Booking
{
    public class GuardianTodayBusRunDto
    {
        public long BookingId { get; set; }
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string? StudentAvatarUrl { get; set; }
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public DateTime ServiceDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public long BusRunId { get; set; }
        public long BusId { get; set; }
        public string BusLabel { get; set; } = null!;
        public long? DriverId { get; set; }
        public string? DriverName { get; set; }
        public long? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int RunOrder { get; set; }
        public string RunStatus { get; set; } = null!;
        public string TodayStatus { get; set; } = null!;
        public long StationId { get; set; }
        public string StationName { get; set; } = null!;
        public string? PickupAddress { get; set; }
        public bool HasCheckedInOnThisBus { get; set; }
        public bool IsCurrentlyOnThisBus { get; set; }
        public long? CurrentBusId { get; set; }
        public string? CurrentBusLabel { get; set; }
        public bool IsOnDifferentBusThanAssigned { get; set; }
    }
}
