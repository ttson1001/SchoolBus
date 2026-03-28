namespace BE_API.Dto.Order
{
    public class OrderPayOsLinkDto
    {
        public long OrderId { get; set; }
        public long GuardianId { get; set; }
        public long StudentId { get; set; }
        public long PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
        public string CheckoutUrl { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
