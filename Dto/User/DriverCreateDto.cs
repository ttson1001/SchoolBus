namespace BE_API.Dto.User
{
    public class DriverCreateDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
        public string? DriverLicenseNumber { get; set; }
        public string? DriverLicenseClass { get; set; }
        public DateTime? DriverLicenseExpiryDate { get; set; }
    }
}
