namespace BE_API.Dto.Account
{
    public class AccountDto
    {
        public long Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? DeviceToken { get; set; }
        public string? Avatar { get; set; }
        public string RoleName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
