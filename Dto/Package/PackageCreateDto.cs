namespace BE_API.Dto.Package
{
    public class PackageCreateDto
    {
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? Type { get; set; }
        public string? ImageUrl { get; set; }
    }
}
