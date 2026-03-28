namespace BE_API.Dto.Order
{
    public class OrderPayOsCreateDto
    {
        public long GuardianId { get; set; }
        public long StudentId { get; set; }
        public long PackageId { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
