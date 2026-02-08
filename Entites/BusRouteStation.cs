namespace BE_API.Entites
{
    public class BusRouteStation : IEntity
    {
        public long Id { get; set; }

        public long RouteId { get; set; }
        public BusRoute Route { get; set; } = null!;

        public long StationId { get; set; }
        public BusStation Station { get; set; } = null!;

        public int OrderIndex { get; set; }
    }

}
