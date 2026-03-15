namespace BE_API.Entites
{
    public class Bus : IEntity
    {
        public long Id { get; set; }
        public string LicensePlate { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsEnabled { get; set; } = true;

        public ICollection<BusDamageReport> DamageReports { get; set; } = new List<BusDamageReport>();
    }
}
