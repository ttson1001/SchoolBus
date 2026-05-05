using BE_API.Configuration;
using BE_API.Database;
using BE_API.Entites;
using BE_API.Entites.Enums;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Extensions
{
    public static class SeedDataExtensions
    {
        private static readonly string[] DefaultRoles =
        {
            "admin",
            "guardian",
            "driver",
            "teacher",
            "staff"
        };
        private const string BookingPickupDistanceMetersKey = "Booking.PickupDistanceMeters";
        private const string DefaultBookingPickupDistanceMetersValue = "500";

        public static async Task EnsureSystemSeedDataAsync(this WebApplication webApp)
        {
            using var scope = webApp.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BeContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");
            var settings = webApp.Configuration.GetSection(SeedAdminSettings.SectionName).Get<SeedAdminSettings>()
                ?? new SeedAdminSettings();

            await EnsureRolesAsync(context, logger);
            await EnsureAdminUserAsync(context, settings, logger);
            await EnsureSystemSettingsAsync(context, logger);
        }

        private static async Task EnsureRolesAsync(BeContext context, ILogger logger)
        {
            var existingRoles = await context.Roles
                .Select(x => x.Name.ToLower())
                .ToListAsync();

            var missingRoles = DefaultRoles
                .Where(role => !existingRoles.Contains(role))
                .Select(role => new Role
                {
                    Name = role
                })
                .ToList();

            if (!missingRoles.Any())
                return;

            await context.Roles.AddRangeAsync(missingRoles);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded roles: {Roles}", string.Join(", ", missingRoles.Select(x => x.Name)));
        }

        private static async Task EnsureAdminUserAsync(BeContext context, SeedAdminSettings settings, ILogger logger)
        {
            var normalizedEmail = settings.Email.Trim().ToLowerInvariant();
            var adminRole = await context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == "admin")
                ?? throw new Exception("Khong tim thay role admin de seed tai khoan quan tri");

            var existingAdmin = await context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);

            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    Email = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(settings.Password),
                    FullName = string.IsNullOrWhiteSpace(settings.FullName) ? "System Admin" : settings.FullName.Trim(),
                    Phone = string.IsNullOrWhiteSpace(settings.Phone) ? null : settings.Phone.Trim(),
                    RoleId = adminRole.Id,
                    Status = AccountStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded default admin account: {Email}", normalizedEmail);
                return;
            }

            var changed = false;

            if (existingAdmin.RoleId != adminRole.Id)
            {
                existingAdmin.RoleId = adminRole.Id;
                changed = true;
            }

            if (existingAdmin.Status != AccountStatus.ACTIVE)
            {
                existingAdmin.Status = AccountStatus.ACTIVE;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existingAdmin.FullName) && !string.IsNullOrWhiteSpace(settings.FullName))
            {
                existingAdmin.FullName = settings.FullName.Trim();
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existingAdmin.Phone) && !string.IsNullOrWhiteSpace(settings.Phone))
            {
                existingAdmin.Phone = settings.Phone.Trim();
                changed = true;
            }

            if (!changed)
                return;

            context.Users.Update(existingAdmin);
            await context.SaveChangesAsync();

            logger.LogInformation("Normalized existing admin account: {Email}", normalizedEmail);
        }

        private static async Task EnsureSystemSettingsAsync(BeContext context, ILogger logger)
        {
            var setting = await context.SystemSettings
                .FirstOrDefaultAsync(x => x.Key == BookingPickupDistanceMetersKey);

            if (setting != null)
                return;

            await context.SystemSettings.AddAsync(new SystemSetting
            {
                Key = BookingPickupDistanceMetersKey,
                Value = DefaultBookingPickupDistanceMetersValue,
                Description = "Khoang cach toi da tu diem don den bus station, tinh theo met"
            });

            await context.SaveChangesAsync();

            logger.LogInformation(
                "Seeded system setting {Key}={Value}",
                BookingPickupDistanceMetersKey,
                DefaultBookingPickupDistanceMetersValue);
        }
    }
}
