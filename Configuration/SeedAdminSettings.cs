namespace BE_API.Configuration
{
    public class SeedAdminSettings
    {
        public const string SectionName = "SeedAdmin";

        public string Email { get; set; } = "admin@schoolbus.local";
        public string Password { get; set; } = "123456";
        public string FullName { get; set; } = "System Admin";
        public string? Phone { get; set; } = "0900000000";
    }
}
