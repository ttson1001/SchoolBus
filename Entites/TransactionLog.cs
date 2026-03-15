namespace BE_API.Entites
{
    public class TransactionLog : IEntity
    {
        public long Id { get; set; }

        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string Method { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public decimal OldBalance { get; set; }
        public decimal NewBalance { get; set; }
        public string? Sender { get; set; }
        public string? Receiver { get; set; }
        public string? Description { get; set; }
        public string? Code { get; set; }
    }
}
