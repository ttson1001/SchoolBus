namespace BE_API.Dto.BusDamageReport
{
    public class BusDamageReportUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
