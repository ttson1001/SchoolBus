namespace BE_API.Entites
{
    public class Package : IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Type { get; set; }
        public string? ImageUrl { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
