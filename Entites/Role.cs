namespace BE_API.Entites
{
    public class Role : IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
