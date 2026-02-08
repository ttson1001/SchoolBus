namespace BE_API.Entites
{
    public class Notification : IEntity
    {
        public long Id { get; set; }

        public long UserId { get; set; }
        public User User { get; set; } = null!;

        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
