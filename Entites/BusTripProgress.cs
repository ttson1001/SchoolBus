namespace BE_API.Entites
{
    public class BusTripProgress : IEntity
    {
        public long Id { get; set; }

        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public long BusScheduleId { get; set; }
        public BusSchedule BusSchedule { get; set; } = null!;

        public long RouteId { get; set; }
        public BusRoute Route { get; set; } = null!;

        public long StationId { get; set; }
        public BusStation Station { get; set; } = null!;

        public DateTime RideDate { get; set; }
        public int OrderIndex { get; set; }
        public DateTime ArrivedAt { get; set; }
    }
}
