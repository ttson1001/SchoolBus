namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressArriveDto
    {
        public long BusId { get; set; }
        public long BusRunId { get; set; }
        public long StationId { get; set; }
        public DateTime? ArrivedAt { get; set; }
    }
}
