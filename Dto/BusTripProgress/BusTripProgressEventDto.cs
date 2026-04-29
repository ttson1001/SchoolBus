namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressEventDto
    {
        public long Id { get; set; }
        public long BusId { get; set; }
        public long BusRunId { get; set; }
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public long StationId { get; set; }
        public string StationName { get; set; } = null!;
        public int OrderIndex { get; set; }
        public DateTime RideDate { get; set; }
        public DateTime ArrivedAt { get; set; }
    }
}
