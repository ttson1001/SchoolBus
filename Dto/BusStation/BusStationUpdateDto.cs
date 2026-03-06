namespace BE_API.Dto.BusStation
{
    public class BusStationUpdateDto
    {
        public string? Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool? IsEnabled { get; set; }
    }
}
