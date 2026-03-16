namespace BE_API.Entites
{
    public class Bus : IEntity
    {
        public long Id { get; set; }
        public string LicensePlate { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public string? BusNumber { get; set; }
        public string? ImageUrl { get; set; }
        public string? Color { get; set; }
        public string? BusType { get; set; }

        public ICollection<BusDamageReport> DamageReports { get; set; } = new List<BusDamageReport>();
    }
}
