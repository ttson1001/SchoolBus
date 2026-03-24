namespace BE_API.Dto.Wallet
{
    public class WalletPayOsCreateDto
    {
        public long UserId { get; set; }
        public decimal Amount { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
