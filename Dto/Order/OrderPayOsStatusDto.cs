namespace BE_API.Dto.Order
{
    public class OrderPayOsStatusDto
    {
        public long OrderId { get; set; }
        public long GuardianId { get; set; }
        public long StudentId { get; set; }
        public long PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public List<long> SelectedRouteIds { get; set; } = new();
        public int PackageRouteLimit { get; set; }
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string OrderStatus { get; set; } = null!;
        public string TransactionStatus { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
