namespace BE_API.Dto.User
{
    public class UserDto
    {
        public long Id { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string RoleName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
