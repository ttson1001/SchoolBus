namespace BE_API.Entites
{
    public class BusAssignment : IEntity
    {
        public long Id { get; set; }

        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public long DriverId { get; set; }
        public User Driver { get; set; } = null!;

        public long TeacherId { get; set; }
        public User Teacher { get; set; } = null!;

        public long RouteId { get; set; }
        public BusRoute Route { get; set; } = null!;

        public DateTime? ActiveDate { get; set; }
    }

}
