namespace BE_API.Dto.BusStation
{
    public class BusStationCreateDto
    {
        public string Name { get; set; } = null!;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
