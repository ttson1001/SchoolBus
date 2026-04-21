namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressCurrentDto
    {
        public long BusId { get; set; }
        public long BusScheduleId { get; set; }
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public DateTime RideDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string TripStatus { get; set; } = null!;
        public long? CurrentStationId { get; set; }
        public string? CurrentStationName { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public long? NextStationId { get; set; }
        public string? NextStationName { get; set; }
        public int? NextOrderIndex { get; set; }
        public bool IsCompleted { get; set; }
        public List<BusTripProgressStationStatusDto> Stations { get; set; } = new();
    }
}
