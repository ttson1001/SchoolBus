namespace BE_API.Dto.TransactionLog
{
    public class TransactionLogCreateDto
    {
        public long? OrderId { get; set; }
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
