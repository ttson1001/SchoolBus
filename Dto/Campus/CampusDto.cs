namespace BE_API.Dto.Campus
{
    public class CampusDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }
    }
}
