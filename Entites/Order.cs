using BE_API.Entites.Enums;

namespace BE_API.Entites
{
    public class Order : IEntity
    {
        public long Id { get; set; }

        public long GuardianId { get; set; }
        public User Guardian { get; set; } = null!;

        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public long BusRouteId { get; set; }
        public BusRoute BusRoute { get; set; } = null!;

        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
