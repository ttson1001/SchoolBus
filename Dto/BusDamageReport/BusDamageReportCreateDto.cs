namespace BE_API.Dto.BusDamageReport
{
    public class BusDamageReportCreateDto
    {
        public long BusId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Status { get; set; }
    }
}
