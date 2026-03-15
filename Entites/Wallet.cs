namespace BE_API.Entites
{
    public class Wallet : IEntity
    {
        public long Id { get; set; }
        public decimal Balance { get; set; }
        public long UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
