using BE_API.Dto.Common;
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

        public async Task<PagedResult<UserDto>> SearchUserAsync(string? keyword, string? role, string? status, int page, int pageSize)
        {
            var query = _userRepo.Get()
                ;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Email.ToLower().Contains(keyword) ||
                    (x.FullName != null && x.FullName.ToLower().Contains(keyword)) ||
                    (x.Phone != null && x.Phone.ToLower().Contains(keyword)) ||
                    x.Role.Name.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                role = role.ToLower();
                query = query.Where(x => x.Role.Name.ToLower() == role);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<AccountStatus>(status, true, out var accountStatus))
                    throw new Exception($"Status '{status}' không hợp lệ.");

                query = query.Where(x => x.Status == accountStatus);
            }

            var totalItems = await query.CountAsync();

            var users = await query.Include(x => x.Role)
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UserDto>
            {
                Items = users.Select(x => MapToUserDto(x, x.Role.Name)).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserDto> GetUserByIdAsync(long id)
        {
            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("User không tồn tại.");

            return MapToUserDto(user, user.Role.Name);
        }

        public async Task<UserImportResultDto> ImportAsync(UserImportRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.File == null || dto.File.Length == 0)
                throw new Exception("Vui lòng chọn file import.");

            if (!Path.GetExtension(dto.File.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ hỗ trợ file Excel (.xlsx).");

            ExcelPackage.License.SetNonCommercialPersonal("SchoolBus");

            var roles = await _roleRepo.Get().ToListAsync(cancellationToken);
            var users = await _userRepo.Get().ToListAsync(cancellationToken);
            var existingEmails = users.Select(x => x.Email.Trim().ToLower()).ToHashSet();

            var result = new UserImportResultDto();
            var newUsers = new List<User>();

            using var stream = dto.File.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet?.Dimension == null)
                throw new Exception("File Excel không có dữ liệu.");

            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;
            var headers = new List<string>();

            for (int col = 1; col <= colCount; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text.Trim().ToLower());
            }

            ValidateHeader(headers);

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
                    var currentRole = FindRole(data, roles, row);

                    if (existingEmails.Contains(email))
                        throw new Exception($"Dòng {row}: email '{email}' đã tồn tại.");

                    var user = new User
                    {
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        FullName = GetOptionalValue(data, "fullname"),
                        Phone = GetOptionalValue(data, "phone"),
                        RoleId = currentRole.Id,
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

        public async Task<UserDto> UpdateUserAsync(long id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new Exception("User không tồn tại.");

            if (dto.Email != null)
            {
                var normalizedEmail = NormalizeRequiredEmail(dto.Email);
                var existedUser = await _userRepo.Get()
                    .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.Id != id, cancellationToken);

                if (existedUser != null)
                    throw new Exception("Email đã tồn tại.");

                user.Email = normalizedEmail;
            }

            if (dto.Password != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Password))
                    throw new Exception("Password không được để trống.");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim());
            }

            if (dto.FullName != null)
                user.FullName = NormalizeOptional(dto.FullName);

            if (dto.Phone != null)
                user.Phone = NormalizeOptional(dto.Phone);

            if (dto.Role != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Role))
                    throw new Exception("Role không được để trống.");

                var normalizedRoleName = dto.Role.Trim().ToLower();
                var currentRole = await _roleRepo.Get()
                    .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedRoleName, cancellationToken)
                    ?? throw new Exception($"Không tìm thấy role '{dto.Role}'.");

                user.RoleId = currentRole.Id;
                user.Role = currentRole;
            }

            if (dto.Status != null)
                user.Status = ParseStatus(dto.Status, 0);

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync(cancellationToken);

            return MapToUserDto(user, user.Role.Name);
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
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password không được để trống.");

            if (string.IsNullOrWhiteSpace(roleName))
                throw new Exception("Role không được để trống.");

            var normalizedEmail = NormalizeRequiredEmail(email);
            var normalizedRoleName = roleName.Trim().ToLower();

            var existedUser = await _userRepo.Get()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (existedUser != null)
                throw new Exception("Email đã tồn tại.");

            var currentRole = await _roleRepo.Get()
                .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedRoleName, cancellationToken)
                ?? throw new Exception($"Không tìm thấy role '{roleName}'.");

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password.Trim()),
                FullName = NormalizeOptional(fullName),
                Phone = NormalizeOptional(phone),
                RoleId = currentRole.Id,
                Status = AccountStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user, cancellationToken);
            await _userRepo.SaveChangesAsync(cancellationToken);

            return (user, currentRole);
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

            if (rowIndex > 0)
                throw new Exception($"Dòng {rowIndex}: status '{status}' không hợp lệ.");

            throw new Exception($"Status '{status}' không hợp lệ.");
        }

        private static string NormalizeRequiredEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Email không được để trống.");

            return email.Trim().ToLower();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
