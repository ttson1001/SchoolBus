using BE_API.Entites.Enums;

namespace BE_API.Entites
{
    public class Payment : IEntity
    {
        public long Id { get; set; }

        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string Method { get; set; } = null!;
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
