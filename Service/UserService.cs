using System.Text;
using BE_API.Dto.User;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

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

            if (!Path.GetExtension(dto.File.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ hỗ trợ file CSV.");

            var roles = await _roleRepo.Get().ToListAsync(cancellationToken);
            var users = await _userRepo.Get().ToListAsync(cancellationToken);
            var existingEmails = users
                .Select(x => x.Email.Trim().ToLower())
                .ToHashSet();

            using var stream = dto.File.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var headerLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(headerLine))
                throw new Exception("File import không có dữ liệu.");

            var headers = headerLine.Split(',')
                .Select(x => x.Trim().ToLower())
                .ToList();

            ValidateHeader(headers);

            var result = new UserImportResultDto();
            var newUsers = new List<User>();
            var rowIndex = 1;

            while (!reader.EndOfStream)
            {
                rowIndex++;
                var line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                result.TotalRows++;

                try
                {
                    var values = line.Split(',').Select(x => x.Trim()).ToList();

                    if (values.Count != headers.Count)
                        throw new Exception($"Dòng {rowIndex}: số cột không hợp lệ.");

                    var data = headers
                        .Select((header, index) => new { header, value = values[index] })
                        .ToDictionary(x => x.header, x => x.value);

                    var email = GetRequiredValue(data, "email", rowIndex).ToLower();
                    var password = GetRequiredValue(data, "password", rowIndex);
                    var role = FindRole(data, roles, rowIndex);

                    if (existingEmails.Contains(email))
                        throw new Exception($"Dòng {rowIndex}: email '{email}' đã tồn tại.");

                    var user = new User
                    {
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        FullName = GetOptionalValue(data, "fullname"),
                        Phone = GetOptionalValue(data, "phone"),
                        RoleId = role.Id,
                        Status = ParseStatus(GetOptionalValue(data, "status"), rowIndex),
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
