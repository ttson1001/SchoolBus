namespace BE_API.Dto.Wallet
{
    public class WalletPayOsLinkDto
    {
        public long UserId { get; set; }
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
        public string CheckoutUrl { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
