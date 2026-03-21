namespace BE_API.Dto.BusRoute
{
    public class BusRouteUpdateDto
    {
        public string? Name { get; set; }
        public bool? IsEnabled { get; set; }
        public long? CampusId { get; set; }
        public List<long>? StationIds { get; set; }
    }
}
