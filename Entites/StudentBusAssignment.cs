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
    }

}
