namespace BE_API.Dto.Order
{
    public class OrderCancelDto
    {
        public string? Reason { get; set; }
        public bool RefundToWallet { get; set; }
    }
}
