namespace BE_API.Dto.Bus
{
    public class BusDto
    {
        public long Id { get; set; }
        public string LicensePlate { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsEnabled { get; set; }
        public string? BusNumber { get; set; }
        public string? ImageUrl { get; set; }
        public string? Color { get; set; }
        public string? BusType { get; set; }
    }
}
