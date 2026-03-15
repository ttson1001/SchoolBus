namespace BE_API.Dto.Package
{
    public class PackageUpdateDto
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public string? ImageUrl { get; set; }
    }
}
