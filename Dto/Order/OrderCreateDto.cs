namespace BE_API.Dto.Order
{
    public class OrderCreateDto
    {
        public long GuardianId { get; set; }
        public long StudentId { get; set; }
        public long PackageId { get; set; }
        public List<long> RouteIds { get; set; } = new();
    }
}
