namespace BE_API.Entites
{
    public class SystemSetting : IEntity
    {
        public long Id { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string? Description { get; set; }
    }
}
