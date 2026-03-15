namespace BE_API.Dto.User
{
    public class UserCreateDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = null!;
    }
}
