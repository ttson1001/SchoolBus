namespace BE_API.Dto.BusRoute
{
    public class BusRouteStationDto
    {
        public long StationId { get; set; }
        public string StationName { get; set; } = null!;
        public int OrderIndex { get; set; }
    }
}
