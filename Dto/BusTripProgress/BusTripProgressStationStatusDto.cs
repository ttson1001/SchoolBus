namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressStationStatusDto
    {
        public long StationId { get; set; }
        public string StationName { get; set; } = null!;
        public int OrderIndex { get; set; }
        public bool IsVisited { get; set; }
        public DateTime? ArrivedAt { get; set; }
    }
}
