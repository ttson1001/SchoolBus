namespace BE_API.Entites
{
    public class StudentBusAssignment : IEntity
    {
        public long Id { get; set; }

        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public long RouteId { get; set; }
        public BusRoute Route { get; set; } = null!;

        public DateTime? RideDate { get; set; }

        public long? PickupStationId { get; set; }
        public BusStation? PickupStation { get; set; }

        public long? DropOffStationId { get; set; }
        public BusStation? DropOffStation { get; set; }

        public string? Note { get; set; }
    }

}
