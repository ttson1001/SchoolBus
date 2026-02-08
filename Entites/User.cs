using BE_API.Entites.Enums;

namespace BE_API.Entites
{
    public class User : IEntity
    {
        public long Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }

        public AccountStatus Status { get; set; } = AccountStatus.ACTIVE;

        public long RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
