namespace BE_API.Entites
{
    public class BusRoute : IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsEnabled { get; set; } = true;
        public long CampusId { get; set; }
        public Campus Campus { get; set; } = null!;
        public ICollection<BusRouteStation> Stations { get; set; } = new List<BusRouteStation>();
    }
}
