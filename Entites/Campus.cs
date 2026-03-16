namespace BE_API.Entites
{
    public class Campus : IEntity
    {
        public long Id { get; set; }

        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
        public string? ImageUrl { get; set; }
        public long? BusId { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<BusRoute> BusRoutes { get; set; } = new List<BusRoute>();
    }
}
