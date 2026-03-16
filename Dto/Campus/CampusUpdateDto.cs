namespace BE_API.Dto.Campus
{
    public class CampusUpdateDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
        public string? ImageUrl { get; set; }
        public long? BusId { get; set; }
    }
}
