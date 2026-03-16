namespace BE_API.Dto.BusStation
{
    public class BusStationDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? Description { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsEnabled { get; set; }
    }
}
