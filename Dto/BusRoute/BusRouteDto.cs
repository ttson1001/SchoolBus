using BE_API.Dto.Bus;

namespace BE_API.Dto.BusRoute
{
    public class BusRouteDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsEnabled { get; set; }
        public long CampusId { get; set; }
        public string CampusName { get; set; } = null!;
        public List<BusDto> Buses { get; set; } = new();
        public List<BusRouteStationDto> Stations { get; set; } = new();
    }
}
