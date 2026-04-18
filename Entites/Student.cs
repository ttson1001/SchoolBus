using BE_API.Entites.Enums;

namespace BE_API.Entites
{
    public class Student : IEntity
    {
        public long Id { get; set; }
        public string StudentCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        public long GuardianId { get; set; }
        public User Guardian { get; set; } = null!;

        public AccountStatus Status { get; set; } = AccountStatus.ACTIVE;

        public long CampusId { get; set; }
        public Campus Campus { get; set; } = null!;
    }

}
