namespace BE_API.Dto.Bus
{
    public class BusCreateDto
    {
        public string LicensePlate { get; set; } = null!;
        public int Capacity { get; set; }
    }
}
