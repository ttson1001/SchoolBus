namespace BE_API.Dto.Wallet
{
    public class WalletDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal Balance { get; set; }
    }
}
