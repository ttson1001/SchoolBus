namespace BE_API.Dto.Bus
{
    public class BusUpdateDto
    {
        public string? LicensePlate { get; set; }
        public int? Capacity { get; set; }
        public bool? IsEnabled { get; set; }
    }
}
