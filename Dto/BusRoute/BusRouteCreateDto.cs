namespace BE_API.Dto.BusRoute
{
    public class BusRouteCreateDto
    {
        public string Name { get; set; } = null!;
        public string? RouteStatus { get; set; }
        public long CampusId { get; set; }
        public List<long> StationIds { get; set; } = new();
    }
}
