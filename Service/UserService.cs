using BE_API.Common;
using BE_API.Dto.Common;
using BE_API.Dto.User;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Net.Mail;

namespace BE_API.Service
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IRepository<BusRun> _busRunRepo;
        private readonly IAppTime _appTime;

        public UserService(IRepository<User> userRepo, IRepository<Role> roleRepo, IRepository<BusRun> busRunRepo, IAppTime appTime)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _busRunRepo = busRunRepo;
            _appTime = appTime;
        }

        public async Task<PagedResult<UserDto>> SearchUserAsync(string? keyword, string? role, string? status, bool? isAssignedToBus, int page, int pageSize)
        {
            IQueryable<User> query = _userRepo.Get().Include(x => x.Role);

            var assignmentQuery = _busRunRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Email.ToLower().Contains(keyword) ||
                    (x.FullName != null && x.FullName.ToLower().Contains(keyword)) ||
                    (x.Phone != null && x.Phone.ToLower().Contains(keyword)) ||
                    (x.DriverLicenseNumber != null && x.DriverLicenseNumber.ToLower().Contains(keyword)) ||
                    (x.DriverLicenseClass != null && x.DriverLicenseClass.ToLower().Contains(keyword)) ||
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

            if (isAssignedToBus.HasValue)
            {
                query = query.Where(x =>
                    x.Role.Name.ToLower() == "driver"
                        ? assignmentQuery.Any(a => a.DriverId == x.Id) == isAssignedToBus.Value
                        : x.Role.Name.ToLower() == "teacher"
                            ? assignmentQuery.Any(a => a.TeacherId == x.Id) == isAssignedToBus.Value
                            : !isAssignedToBus.Value);
            }

            var totalItems = await query.CountAsync();

            var users = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = users.Select(x => x.Id).ToList();
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
            var existingPhones = users
                .Where(x => !string.IsNullOrWhiteSpace(x.Phone))
                .Select(x => x.Phone!.Trim())
                .ToHashSet();

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
                headers.Add(worksheet.Cells[1, col].Text.Trim().ToLower());

            ValidateHeader(headers);

            for (int row = 2; row <= rowCount; row++)
            {
                result.TotalRows++;

                try
                {
                    var data = new Dictionary<string, string>();

                    for (int col = 1; col <= colCount; col++)
                        data[headers[col - 1]] = worksheet.Cells[row, col].Text.Trim();

                    var email = NormalizeRequiredEmail(GetRequiredValue(data, "email", row));
                    var password = GetRequiredValue(data, "password", row);
                    var phone = NormalizePhone(GetOptionalValue(data, "phone"));
                    var currentRole = FindRole(data, roles, row);

                    if (existingEmails.Contains(email))
                        throw new Exception($"Dòng {row}: email '{email}' đã tồn tại.");

                    if (!string.IsNullOrWhiteSpace(phone) && existingPhones.Contains(phone))
                        throw new Exception($"Dòng {row}: số điện thoại '{phone}' đã tồn tại.");

                    var user = new User
                    {
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        FullName = GetOptionalValue(data, "fullname"),
                        AvatarUrl = null,
                        Phone = phone,
                        RoleId = currentRole.Id,
                        Status = ParseStatus(GetOptionalValue(data, "status"), row),
                        CreatedAt = DateTime.UtcNow
                    };

                    newUsers.Add(user);
                    existingEmails.Add(email);
                    if (!string.IsNullOrWhiteSpace(phone))
                        existingPhones.Add(phone);
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
                dto.AvatarUrl,
                dto.Phone,
                null,
                null,
                null,
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

            if (dto.AvatarUrl != null)
                user.AvatarUrl = NormalizeAvatarUrl(dto.AvatarUrl);

            if (dto.Phone != null)
            {
                var normalizedPhone = NormalizePhone(dto.Phone);

                if (!string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    var existedPhone = await _userRepo.Get()
                        .FirstOrDefaultAsync(x =>
                            x.Phone != null &&
                            x.Phone == normalizedPhone &&
                            x.Id != id, cancellationToken);

                    if (existedPhone != null)
                        throw new Exception("Số điện thoại đã tồn tại.");
                }

                user.Phone = normalizedPhone;
            }

            if (dto.DeviceToken != null)
                user.DeviceToken = NormalizeOptional(dto.DeviceToken);

            if (dto.DriverLicenseNumber != null)
            {
                var normalizedDriverLicenseNumber = NormalizeDriverLicenseNumber(dto.DriverLicenseNumber);

                if (!string.IsNullOrWhiteSpace(normalizedDriverLicenseNumber))
                {
                    var existedDriverLicense = await _userRepo.Get()
                        .FirstOrDefaultAsync(x =>
                            x.DriverLicenseNumber != null &&
                            x.DriverLicenseNumber.ToLower() == normalizedDriverLicenseNumber.ToLower() &&
                            x.Id != id, cancellationToken);

                    if (existedDriverLicense != null)
                        throw new Exception("Số bằng lái đã tồn tại.");
                }

                user.DriverLicenseNumber = normalizedDriverLicenseNumber;
            }

            if (dto.DriverLicenseClass != null)
                user.DriverLicenseClass = NormalizeOptional(dto.DriverLicenseClass);

            if (dto.DriverLicenseExpiryDate.HasValue)
            {
                if (dto.DriverLicenseExpiryDate.Value.Date <= _appTime.TodayDate)
                    throw new Exception("Hạn bằng lái phải lớn hơn ngày hiện tại.");

                user.DriverLicenseExpiryDate = dto.DriverLicenseExpiryDate.Value.Date;
            }

            if (dto.Role != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Role))
                    throw new Exception("Role không được để trống.");

                var normalizedRoleName = dto.Role.Trim().ToLower();
                var currentRole = await _roleRepo.Get()
                    .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedRoleName, cancellationToken)
                    ?? throw new Exception($"Không tìm thấy role '{dto.Role}'.");

                if (normalizedRoleName == "driver" && string.IsNullOrWhiteSpace(user.DriverLicenseNumber))
                    throw new Exception("Driver phải có số bằng lái.");

                user.RoleId = currentRole.Id;
                user.Role = currentRole;
            }

            if (dto.Status != null)
                user.Status = ParseStatus(dto.Status, 0);

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync(cancellationToken);

            return MapToUserDto(user, user.Role.Name);
        }

        public async Task<UserDto> DeleteUserAsync(long id, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new Exception("User không tồn tại.");

            if (user.Role != null && user.Role.Name.ToLower() == "admin")
            {
                throw new Exception("Không thể disable tài khoản ADMIN.");
            }

            user.Status = AccountStatus.DISABLED;

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
                dto.AvatarUrl,
                dto.Phone,
                null,
                null,
                null,
                "teacher",
                cancellationToken);

            return new TeacherDto
            {
                Id = createdUser.user.Id,
                Email = createdUser.user.Email,
                FullName = createdUser.user.FullName,
                AvatarUrl = createdUser.user.AvatarUrl,
                Phone = createdUser.user.Phone,
                DeviceToken = createdUser.user.DeviceToken,
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
                dto.AvatarUrl,
                dto.Phone,
                dto.DriverLicenseNumber,
                dto.DriverLicenseClass,
                dto.DriverLicenseExpiryDate,
                "driver",
                cancellationToken);

            return new DriverDto
            {
                Id = createdUser.user.Id,
                Email = createdUser.user.Email,
                FullName = createdUser.user.FullName,
                AvatarUrl = createdUser.user.AvatarUrl,
                Phone = createdUser.user.Phone,
                DeviceToken = createdUser.user.DeviceToken,
                DriverLicenseNumber = createdUser.user.DriverLicenseNumber,
                DriverLicenseClass = createdUser.user.DriverLicenseClass,
                DriverLicenseExpiryDate = createdUser.user.DriverLicenseExpiryDate,
                RoleName = createdUser.role.Name,
                Status = createdUser.user.Status.ToString(),
                CreatedAt = createdUser.user.CreatedAt
            };
        }

        private async Task<(User user, Role role)> CreateUserInternalAsync(
            string email,
            string password,
            string? fullName,
            string? avatarUrl,
            string? phone,
            string? driverLicenseNumber,
            string? driverLicenseClass,
            DateTime? driverLicenseExpiryDate,
            string roleName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password không được để trống.");

            if (string.IsNullOrWhiteSpace(roleName))
                throw new Exception("Role không được để trống.");

            var normalizedEmail = NormalizeRequiredEmail(email);
            var normalizedPhone = NormalizePhone(phone);
            var normalizedRoleName = roleName.Trim().ToLower();
            var normalizedDriverLicenseNumber = NormalizeDriverLicenseNumber(driverLicenseNumber);
            var normalizedDriverLicenseClass = NormalizeOptional(driverLicenseClass);

            var existedUser = await _userRepo.Get()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);

            if (existedUser != null)
                throw new Exception("Email đã tồn tại.");

            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                var existedPhone = await _userRepo.Get()
                    .FirstOrDefaultAsync(x => x.Phone != null && x.Phone == normalizedPhone, cancellationToken);

                if (existedPhone != null)
                    throw new Exception("Số điện thoại đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedDriverLicenseNumber))
            {
                var existedDriverLicense = await _userRepo.Get()
                    .FirstOrDefaultAsync(x =>
                        x.DriverLicenseNumber != null &&
                        x.DriverLicenseNumber.ToLower() == normalizedDriverLicenseNumber.ToLower(), cancellationToken);

                if (existedDriverLicense != null)
                    throw new Exception("Số bằng lái đã tồn tại.");
            }

            var currentRole = await _roleRepo.Get()
                .FirstOrDefaultAsync(x => x.Name.ToLower() == normalizedRoleName, cancellationToken)
                ?? throw new Exception($"Không tìm thấy role '{roleName}'.");

            if (normalizedRoleName == "driver" && string.IsNullOrWhiteSpace(normalizedDriverLicenseNumber))
                throw new Exception("Driver phải có số bằng lái.");

            if (driverLicenseExpiryDate.HasValue && driverLicenseExpiryDate.Value.Date <= _appTime.TodayDate)
                throw new Exception("Hạn bằng lái phải lớn hơn ngày hiện tại.");

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password.Trim()),
                FullName = NormalizeOptional(fullName),
                AvatarUrl = NormalizeAvatarUrl(avatarUrl),
                Phone = normalizedPhone,
                DriverLicenseNumber = normalizedDriverLicenseNumber,
                DriverLicenseClass = normalizedDriverLicenseClass,
                DriverLicenseExpiryDate = driverLicenseExpiryDate?.Date,
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
                AvatarUrl = user.AvatarUrl,
                Phone = user.Phone,
                DeviceToken = user.DeviceToken,
                DriverLicenseNumber = user.DriverLicenseNumber,
                DriverLicenseClass = user.DriverLicenseClass,
                DriverLicenseExpiryDate = user.DriverLicenseExpiryDate,
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

            var normalizedEmail = email.Trim().ToLower();

            try
            {
                _ = new MailAddress(normalizedEmail);
            }
            catch
            {
                throw new Exception("Email không hợp lệ.");
            }

            return normalizedEmail;
        }

        private static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var normalizedPhone = phone.Trim();

            if (normalizedPhone.Length < 9 || normalizedPhone.Length > 15 || normalizedPhone.Any(x => !char.IsDigit(x)))
                throw new Exception("Số điện thoại không hợp lệ.");

            return normalizedPhone;
        }

        private static string? NormalizeDriverLicenseNumber(string? driverLicenseNumber)
        {
            if (string.IsNullOrWhiteSpace(driverLicenseNumber))
                return null;

            var normalizedDriverLicenseNumber = driverLicenseNumber.Trim();

            if (normalizedDriverLicenseNumber.Length > 50)
                throw new Exception("Số bằng lái không hợp lệ.");

            return normalizedDriverLicenseNumber;
        }

        private static string? NormalizeAvatarUrl(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return null;

            var normalizedAvatarUrl = avatarUrl.Trim();

            if (normalizedAvatarUrl.Length > 1000)
                throw new Exception("AvatarUrl không hợp lệ.");

            return normalizedAvatarUrl;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
