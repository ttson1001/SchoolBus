namespace BE_API.Dto.BusDamageReport
{
    public class BusDamageReportDto
    {
        public long Id { get; set; }
        public long BusId { get; set; }
        public string BusLicensePlate { get; set; } = null!;
        public long? ReportedByUserId { get; set; }
        public string? ReportedByName { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public DateTime ReportedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
