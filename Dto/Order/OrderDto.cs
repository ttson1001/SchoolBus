namespace BE_API.Dto.Order
{
    public class OrderDto
    {
        public long Id { get; set; }
        public long GuardianId { get; set; }
        public string GuardianName { get; set; } = null!;
        public long StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public long BusRouteId { get; set; }
        public string BusRouteName { get; set; } = null!;
        public long PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public decimal PackagePrice { get; set; }
        public int DurationDays { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
