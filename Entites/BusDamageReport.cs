namespace BE_API.Entites
{
    public class BusDamageReport : IEntity
    {
        public long Id { get; set; }

        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
