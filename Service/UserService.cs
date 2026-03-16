using BE_API.Dto.User;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace BE_API.Service
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;

        public UserService(IRepository<User> userRepo, IRepository<Role> roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
        }
        public async Task<UserImportResultDto> ImportAsync(UserImportRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.File == null || dto.File.Length == 0)
                throw new Exception("Vui lòng chọn file import.");

            if (!Path.GetExtension(dto.File.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ hỗ trợ file Excel (.xlsx).");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var roles = await _roleRepo.Get().ToListAsync(cancellationToken);
            var users = await _userRepo.Get().ToListAsync(cancellationToken);

            var existingEmails = users
                .Select(x => x.Email.Trim().ToLower())
                .ToHashSet();

            var result = new UserImportResultDto();
            var newUsers = new List<User>();

            using var stream = dto.File.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                throw new Exception("File Excel không có dữ liệu.");

            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            // đọc header
            var headers = new List<string>();

            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text.Trim().ToLower());
            }

            ValidateHeader(headers);

            // đọc data
            for (int row = 2; row <= rowCount; row++)
            {
                result.TotalRows++;

                try
                {
                    var data = new Dictionary<string, string>();

                    for (int col = 1; col <= colCount; col++)
                    {
                        data[headers[col - 1]] = worksheet.Cells[row, col].Text.Trim();
                    }

                    var email = GetRequiredValue(data, "email", row).ToLower();
                    var password = GetRequiredValue(data, "password", row);
                    var role = FindRole(data, roles, row);

                    if (existingEmails.Contains(email))
                        throw new Exception($"Dòng {row}: email '{email}' đã tồn tại.");

                    var user = new User
                    {
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        FullName = GetOptionalValue(data, "fullname"),
                        Phone = GetOptionalValue(data, "phone"),
                        RoleId = role.Id,
                        Status = ParseStatus(GetOptionalValue(data, "status"), row),
                        CreatedAt = DateTime.UtcNow
                    };

                    newUsers.Add(user);
                    existingEmails.Add(email);

                    result.SuccessRows++;
                }
                catch (Exception ex)
                {
                    result.FailedRows++;
                    result.Errors.Add(ex.Message);
                }
            }

            if (newUsers.Any())
            {
                await _userRepo.AddRangeAsync(newUsers, cancellationToken);
                await _userRepo.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        public async Task<UserDto> CreateUserAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
        {
            var createdUser = await CreateUserInternalAsync(
                dto.Email,
                dto.Password,
                dto.FullName,
                dto.Phone,
                dto.Role,
                cancellationToken);

            return MapToUserDto(createdUser.user, createdUser.role.Name);
        }

        public async Task<TeacherDto> CreateTeacherAsync(TeacherCreateDto dto, CancellationToken cancellationToken = default)
        {
            var createdUser = await CreateUserInternalAsync(
                dto.Email,
                dto.Password,
                dto.FullName,
                dto.Phone,
                "teacher",
                cancellationToken);

            return new TeacherDto
            {
                Id = createdUser.user.Id,
                Email = createdUser.user.Email,
                FullName = createdUser.user.FullName,
                Phone = createdUser.user.Phone,
                RoleName = createdUser.role.Name,
                Status = createdUser.user.Status.ToString(),
                CreatedAt = createdUser.user.CreatedAt
            };
        }

        public async Task<DriverDto> CreateDriverAsync(DriverCreateDto dto, CancellationToken cancellationToken = default)
        {
            var createdUser = await CreateUserInternalAsync(
                dto.Email,
                dto.Password,
                dto.FullName,
                dto.Phone,
                "driver",
                cancellationToken);

            return new DriverDto
            {
                Id = createdUser.user.Id,
                Email = createdUser.user.Email,
                FullName = createdUser.user.FullName,
                Phone = createdUser.user.Phone,
                RoleName = createdUser.role.Name,
                Status = createdUser.user.Status.ToString(),
                CreatedAt = createdUser.user.CreatedAt
            };
        }

        private async Task<(User user, Role role)> CreateUserInternalAsync(
            string email,
            string password,
            string? fullName,
            string? phone,
            string roleName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Email không được để trống.");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password không được để trống.");

            if (string.IsNullOrWhiteSpace(roleName))
                throw new Exception("Role không được để trống.");

            var normalizedEmail = email.Trim().ToLower();
            var normalizedRoleName = roleName.Trim().ToLower();

            var existedUser = await _userRepo.Get()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (existedUser != null)
                throw new Exception("Email đã tồn tại.");

            var role = await _roleRepo.Get()
                .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedRoleName, cancellationToken)
                ?? throw new Exception($"Không tìm thấy role '{roleName}'.");

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password.Trim()),
                FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                RoleId = role.Id,
                Status = AccountStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user, cancellationToken);
            await _userRepo.SaveChangesAsync(cancellationToken);

            return (user, role);
        }

        private static UserDto MapToUserDto(User user, string roleName)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                RoleName = roleName,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            };
        }

        private static void ValidateHeader(List<string> headers)
        {
            if (!headers.Contains("email"))
                throw new Exception("File import thiếu cột email.");

            if (!headers.Contains("password"))
                throw new Exception("File import thiếu cột password.");

            if (!headers.Contains("roleid") && !headers.Contains("rolename"))
                throw new Exception("File import cần có roleId hoặc roleName.");
        }

        private static string GetRequiredValue(Dictionary<string, string> data, string key, int rowIndex)
        {
            if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                throw new Exception($"Dòng {rowIndex}: {key} không được để trống.");

            return value.Trim();
        }

        private static string? GetOptionalValue(Dictionary<string, string> data, string key)
        {
            if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private static Role FindRole(Dictionary<string, string> data, List<Role> roles, int rowIndex)
        {
            var roleIdText = GetOptionalValue(data, "roleid");
            if (!string.IsNullOrWhiteSpace(roleIdText))
            {
                if (!long.TryParse(roleIdText, out var roleId))
                    throw new Exception($"Dòng {rowIndex}: roleId không hợp lệ.");

                return roles.FirstOrDefault(x => x.Id == roleId)
                    ?? throw new Exception($"Dòng {rowIndex}: không tìm thấy roleId {roleId}.");
            }

            var roleName = GetOptionalValue(data, "rolename");
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                return roles.FirstOrDefault(x => x.Name.ToLower() == roleName.ToLower())
                    ?? throw new Exception($"Dòng {rowIndex}: không tìm thấy roleName '{roleName}'.");
            }

            throw new Exception($"Dòng {rowIndex}: thiếu roleId hoặc roleName.");
        }

        private static AccountStatus ParseStatus(string? status, int rowIndex)
        {
            if (string.IsNullOrWhiteSpace(status))
                return AccountStatus.ACTIVE;

            if (Enum.TryParse<AccountStatus>(status, true, out var accountStatus))
                return accountStatus;

            throw new Exception($"Dòng {rowIndex}: status '{status}' không hợp lệ.");
        }
    }
}
