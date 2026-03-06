namespace BE_API.Dto.Bus
{
    public class BusDto
    {
        public long Id { get; set; }
        public string LicensePlate { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsEnabled { get; set; }
    }
}
