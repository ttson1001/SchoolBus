namespace BE_API.Dto.Campus
{
    public class CampusCreateDto
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? Phone { get; set; }
    }
}
