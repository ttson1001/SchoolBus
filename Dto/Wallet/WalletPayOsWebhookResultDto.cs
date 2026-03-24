namespace BE_API.Dto.Wallet
{
    public class WalletPayOsWebhookResultDto
    {
        public long UserId { get; set; }
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public decimal WalletBalance { get; set; }
    }
}
