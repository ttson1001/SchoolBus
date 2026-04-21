using BE_API.Common;
using BE_API.Dto.Common;
using BE_API.Dto.Student;
using BE_API.Dto.User;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;

namespace BE_API.Service
{
    public class StudentService : IStudentService
    {
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Campus> _campusRepo;
        private readonly IAppTime _appTime;

        public StudentService(
            IRepository<Student> studentRepo,
            IRepository<User> userRepo,
            IRepository<Campus> campusRepo,
            IAppTime appTime)
        {
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _campusRepo = campusRepo;
            _appTime = appTime;
        }

        public async Task<PagedResult<StudentDto>> SearchStudentAsync(string? keyword, long? campusId, long? guardianId, string? status, int page, int pageSize)
        {
            IQueryable<Student> query = _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(x =>
                    x.StudentCode.ToLower().Contains(keyword) ||
                    x.FullName.ToLower().Contains(keyword) ||
                    (x.Guardian != null && x.Guardian.FullName != null && x.Guardian.FullName.ToLower().Contains(keyword)) ||
                    (x.Campus != null && x.Campus.Name != null && x.Campus.Name.ToLower().Contains(keyword)));
            }

            if (campusId.HasValue)
                query = query.Where(x => x.CampusId == campusId.Value);

            if (guardianId.HasValue)
                query = query.Where(x => x.GuardianId == guardianId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<AccountStatus>(status, true, out var accountStatus))
                    throw new Exception($"Status '{status}' không hợp lệ.");

                query = query.Where(x => x.Status == accountStatus);
            }

            var totalItems = await query.CountAsync();

            var students = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<StudentDto>
            {
                Items = students.Select(MapToStudentDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<StudentDto> GetStudentByIdAsync(long id)
        {
            var student = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student không tồn tại");

            return MapToStudentDto(student);
        }

        public async Task<StudentDetailDto> GetStudentByCodeAsync(string studentCode)
        {
            if (string.IsNullOrWhiteSpace(studentCode))
                throw new Exception("StudentCode không được để trống");

            var normalizedStudentCode = studentCode.Trim().ToLower();

            var student = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .ThenInclude(x => x.Role)
                .Include(x => x.Campus)
                .FirstOrDefaultAsync(x => x.StudentCode.ToLower() == normalizedStudentCode)
                ?? throw new Exception("Student không tồn tại");

            return MapToStudentDetailDto(student);
        }

        public async Task<List<StudentDto>> GetStudentsByCampusIdAsync(long campusId)
        {
            await ValidateCampusAsync(campusId);

            var students = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus)
                .Where(x => x.CampusId == campusId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return students.Select(MapToStudentDto).ToList();
        }

        public async Task<List<StudentDto>> GetStudentsByGuardianIdAsync(long guardianId)
        {
            await ValidateGuardianAsync(guardianId);

            var students = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus)
                .Where(x => x.GuardianId == guardianId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return students.Select(MapToStudentDto).ToList();
        }

        public async Task<List<StudentDto>> GetStudentsByGuardianPhoneAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new Exception("PhoneNumber không được để trống");

            var normalizedPhoneNumber = phoneNumber.Trim();

            var guardian = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.Phone != null &&
                    x.Phone.Trim().ToLower() == normalizedPhoneNumber.ToLower())
                ?? throw new Exception("Không tìm thấy guardian với số điện thoại đã cung cấp");

            if (!string.Equals(guardian.Role.Name, "guardian", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Số điện thoại này không thuộc tài khoản guardian");

            if (guardian.Status != AccountStatus.ACTIVE)
                throw new Exception("Guardian đang không hoạt động");

            var students = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus)
                .Where(x => x.GuardianId == guardian.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return students.Select(MapToStudentDto).ToList();
        }

        public async Task<StudentImportResultDto> ImportByGuardianEmailAsync(StudentImportByGuardianEmailRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.File == null || dto.File.Length == 0)
                throw new Exception("Vui lòng chọn file import.");

            if (!Path.GetExtension(dto.File.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Chỉ hỗ trợ file Excel (.xlsx).");

            ExcelPackage.License.SetNonCommercialPersonal("SchoolBus");

            var users = await _userRepo.Get()
                .Include(x => x.Role)
                .ToListAsync(cancellationToken);
            var campuses = await _campusRepo.Get().ToListAsync(cancellationToken);
            var existingStudentCodes = (await _studentRepo.Get()
                    .Select(x => x.StudentCode)
                    .ToListAsync(cancellationToken))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLower())
                .ToHashSet();

            var duplicateIdentityKeys = new HashSet<string>();
            var result = new StudentImportResultDto();
            var newStudents = new List<Student>();

            using var stream = dto.File.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet?.Dimension == null)
                throw new Exception("File Excel không có dữ liệu.");

            var headers = new List<string>();
            var columnCount = worksheet.Dimension.Columns;

            for (var column = 1; column <= columnCount; column++)
                headers.Add(worksheet.Cells[1, column].Text.Trim().ToLower());

            ValidateStudentImportHeaders(headers);

            for (var rowIndex = 2; rowIndex <= worksheet.Dimension.Rows; rowIndex++)
            {
                var rowValues = new List<string>();
                for (var column = 1; column <= columnCount; column++)
                    rowValues.Add(worksheet.Cells[rowIndex, column].Text.Trim());

                if (rowValues.All(string.IsNullOrWhiteSpace))
                    continue;

                result.TotalRows++;

                try
                {
                    var data = BuildImportRowData(headers, rowValues, rowIndex);

                    var studentCode = NormalizeStudentCode(GetRequiredImportValue(data, "studentcode", rowIndex));
                    var fullName = NormalizeRequiredFullName(GetRequiredImportValue(data, "fullname", rowIndex));
                    var dateOfBirth = ParseImportDateOfBirth(GetRequiredImportValue(data, "dateofbirth", rowIndex), rowIndex);
                    var gender = NormalizeGender(GetRequiredImportValue(data, "gender", rowIndex));
                    var guardianEmail = NormalizeGuardianEmail(GetRequiredImportValue(data, "guardianemail", rowIndex));
                    var campusId = ParseImportCampusId(GetRequiredImportValue(data, "campusid", rowIndex), rowIndex);
                    var avatarUrl = NormalizeAvatarUrl(GetOptionalImportValue(data, "avatarurl"));

                    if (!existingStudentCodes.Add(studentCode.ToLower()))
                        throw new Exception($"Dòng {rowIndex}: mã học sinh '{studentCode}' đã tồn tại.");

                    var guardian = users.FirstOrDefault(x =>
                        x.Email.ToLower() == guardianEmail &&
                        x.Role != null &&
                        x.Role.Name.Equals("guardian", StringComparison.OrdinalIgnoreCase))
                        ?? throw new Exception($"Dòng {rowIndex}: không tìm thấy guardian với email '{guardianEmail}'.");

                    if (guardian.Status != AccountStatus.ACTIVE)
                        throw new Exception($"Dòng {rowIndex}: guardian '{guardianEmail}' đang không hoạt động.");

                    var campus = campuses.FirstOrDefault(x => x.Id == campusId)
                        ?? throw new Exception($"Dòng {rowIndex}: campusId {campusId} không tồn tại.");

                    if (!campus.IsActive)
                        throw new Exception($"Dòng {rowIndex}: campusId {campusId} đang không hoạt động.");

                    var duplicateIdentityKey = BuildDuplicateStudentIdentityKey(fullName, dateOfBirth, gender, guardian.Id, campusId);
                    if (!duplicateIdentityKeys.Add(duplicateIdentityKey))
                        throw new Exception($"Dòng {rowIndex}: student bị trùng thông tin trong file import.");

                    await EnsureStudentNotDuplicatedAsync(
                        fullName,
                        dateOfBirth,
                        gender,
                        guardian.Id,
                        campusId);

                    newStudents.Add(new Student
                    {
                        StudentCode = studentCode,
                        FullName = fullName,
                        AvatarUrl = avatarUrl,
                        DateOfBirth = dateOfBirth,
                        Gender = gender,
                        GuardianId = guardian.Id,
                        CampusId = campusId,
                        Status = AccountStatus.ACTIVE
                    });

                    result.SuccessRows++;
                }
                catch (Exception ex)
                {
                    result.FailedRows++;
                    result.Errors.Add(ex.Message);
                }
            }

            if (newStudents.Any())
            {
                await _studentRepo.AddRangeAsync(newStudents, cancellationToken);
                await _studentRepo.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        public async Task CreateStudentAsync(StudentCreateDto dto)
        {
            var studentCode = NormalizeStudentCode(dto.StudentCode);
            var normalizedFullName = NormalizeRequiredFullName(dto.FullName);
            var avatarUrl = NormalizeAvatarUrl(dto.AvatarUrl);
            var dateOfBirth = ValidateDateOfBirth(dto.DateOfBirth);
            var gender = NormalizeGender(dto.Gender);

            await ValidateGuardianAsync(dto.GuardianId);
            await ValidateCampusAsync(dto.CampusId);
            await EnsureStudentCodeUniqueAsync(studentCode);
            await EnsureStudentNotDuplicatedAsync(
                normalizedFullName,
                dateOfBirth,
                gender,
                dto.GuardianId,
                dto.CampusId);

            var student = new Student
            {
                StudentCode = studentCode,
                FullName = normalizedFullName,
                AvatarUrl = avatarUrl,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                GuardianId = dto.GuardianId,
                CampusId = dto.CampusId,
                Status = AccountStatus.ACTIVE
            };

            await _studentRepo.AddAsync(student);
            await _studentRepo.SaveChangesAsync();
        }

        public async Task<StudentDto> UpdateStudentAsync(long id, StudentUpdateDto dto)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student không tồn tại");

            if (dto.StudentCode != null)
            {
                student.StudentCode = NormalizeStudentCode(dto.StudentCode);
                await EnsureStudentCodeUniqueAsync(student.StudentCode, id);
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                student.FullName = NormalizeRequiredFullName(dto.FullName);

            if (dto.AvatarUrl != null)
                student.AvatarUrl = NormalizeAvatarUrl(dto.AvatarUrl);

            if (dto.DateOfBirth.HasValue)
                student.DateOfBirth = ValidateDateOfBirth(dto.DateOfBirth);

            if (!string.IsNullOrWhiteSpace(dto.Gender))
                student.Gender = NormalizeGender(dto.Gender);

            if (dto.GuardianId.HasValue)
            {
                await ValidateGuardianAsync(dto.GuardianId.Value);
                student.GuardianId = dto.GuardianId.Value;
            }

            if (dto.CampusId.HasValue)
            {
                await ValidateCampusAsync(dto.CampusId.Value);
                student.CampusId = dto.CampusId.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                if (!Enum.TryParse<AccountStatus>(dto.Status, true, out var accountStatus))
                    throw new Exception($"Status '{dto.Status}' không hợp lệ.");

                student.Status = accountStatus;
            }

            await EnsureStudentNotDuplicatedAsync(
                student.FullName,
                student.DateOfBirth,
                student.Gender,
                student.GuardianId,
                student.CampusId,
                id);

            _studentRepo.Update(student);
            await _studentRepo.SaveChangesAsync();

            var updatedStudent = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student không tồn tại");

            return MapToStudentDto(updatedStudent);
        }

        public async Task DeleteStudentAsync(long id)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student không tồn tại");

            _studentRepo.Delete(student);
            await _studentRepo.SaveChangesAsync();
        }

        private async Task ValidateGuardianAsync(long guardianId)
        {
            if (guardianId <= 0)
                throw new Exception("GuardianId phải lớn hơn 0");

            var guardian = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == guardianId)
                ?? throw new Exception("Guardian không tồn tại");

            if (!string.Equals(guardian.Role.Name, "guardian", StringComparison.OrdinalIgnoreCase))
                throw new Exception("User được chọn không phải guardian");

            if (guardian.Status != AccountStatus.ACTIVE)
                throw new Exception("Guardian đang không hoạt động");
        }

        private async Task ValidateCampusAsync(long campusId)
        {
            if (campusId <= 0)
                throw new Exception("CampusId phải lớn hơn 0");

            var campus = await _campusRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == campusId)
                ?? throw new Exception("Campus không tồn tại");

            if (!campus.IsActive)
                throw new Exception("Campus đang không hoạt động");
        }

        private async Task EnsureStudentCodeUniqueAsync(string studentCode, long? excludedStudentId = null)
        {
            var exists = await _studentRepo.Get()
                .AnyAsync(x =>
                    x.StudentCode.ToLower() == studentCode.ToLower() &&
                    (!excludedStudentId.HasValue || x.Id != excludedStudentId.Value));

            if (exists)
                throw new Exception("Mã học sinh đã tồn tại");
        }

        private static void ValidateStudentImportHeaders(List<string> headers)
        {
            var requiredHeaders = new[]
            {
                "studentcode",
                "fullname",
                "dateofbirth",
                "gender",
                "guardianemail",
                "campusid"
            };

            foreach (var header in requiredHeaders)
            {
                if (!headers.Contains(header))
                    throw new Exception($"File import thiếu cột {header}.");
            }
        }

        private static Dictionary<string, string> BuildImportRowData(List<string> headers, List<string> values, int rowIndex)
        {
            if (values.Count > headers.Count)
                throw new Exception($"Dòng {rowIndex}: số cột dữ liệu nhiều hơn header.");

            var data = new Dictionary<string, string>();

            for (var i = 0; i < headers.Count; i++)
                data[headers[i]] = i < values.Count ? values[i].Trim() : string.Empty;

            return data;
        }

        private static string GetRequiredImportValue(Dictionary<string, string> data, string key, int rowIndex)
        {
            if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                throw new Exception($"Dòng {rowIndex}: {key} không được để trống.");

            return value.Trim();
        }

        private static string? GetOptionalImportValue(Dictionary<string, string> data, string key)
        {
            if (!data.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private static string NormalizeGuardianEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("GuardianEmail không được để trống");

            return email.Trim().ToLower();
        }

        private DateTime ParseImportDateOfBirth(string value, int rowIndex)
        {
            if (!DateTime.TryParse(value, out var parsedDate))
                throw new Exception($"Dòng {rowIndex}: dateOfBirth '{value}' không hợp lệ.");

            return ValidateDateOfBirth(parsedDate);
        }

        private static long ParseImportCampusId(string value, int rowIndex)
        {
            if (!long.TryParse(value, out var campusId) || campusId <= 0)
                throw new Exception($"Dòng {rowIndex}: campusId '{value}' không hợp lệ.");

            return campusId;
        }

        private static string BuildDuplicateStudentIdentityKey(string fullName, DateTime dateOfBirth, string gender, long guardianId, long campusId)
        {
            return $"{fullName.Trim().ToLower()}|{dateOfBirth:yyyy-MM-dd}|{gender.Trim().ToLower()}|{guardianId}|{campusId}";
        }

        private static string NormalizeStudentCode(string? studentCode)
        {
            if (string.IsNullOrWhiteSpace(studentCode))
                throw new Exception("StudentCode không được để trống");

            var normalizedStudentCode = studentCode.Trim();

            if (normalizedStudentCode.Length > 50)
                throw new Exception("StudentCode không được vượt quá 50 ký tự");

            return normalizedStudentCode;
        }

        private static string NormalizeRequiredFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new Exception("FullName không được để trống");

            var normalizedFullName = fullName.Trim();

            if (normalizedFullName.Length > 100)
                throw new Exception("FullName không được vượt quá 100 ký tự");

            if (normalizedFullName.Any(char.IsDigit))
                throw new Exception("Tên học sinh không được chứa số");

            return normalizedFullName;
        }

        private static string? NormalizeAvatarUrl(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return null;

            var normalizedAvatarUrl = avatarUrl.Trim();

            if (normalizedAvatarUrl.Length > 1000)
                throw new Exception("AvatarUrl không được vượt quá 1000 ký tự");

            return normalizedAvatarUrl;
        }

        private DateTime ValidateDateOfBirth(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
                throw new Exception("DateOfBirth không được để trống");

            var normalizedDate = dateOfBirth.Value.Date;
            var today = _appTime.TodayDate;

            if (normalizedDate >= today)
                throw new Exception("DateOfBirth phải nhỏ hơn ngày hiện tại");

            return normalizedDate;
        }

        private static string NormalizeGender(string? gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
                throw new Exception("Gender không được để trống");

            var normalizedGender = gender.Trim().ToLowerInvariant();
            var allowedGenders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "male",
                "female",
                "other"
            };

            if (!allowedGenders.Contains(normalizedGender))
                throw new Exception("Gender chỉ chấp nhận male, female hoặc other");

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalizedGender);
        }

        private async Task EnsureStudentNotDuplicatedAsync(
            string fullName,
            DateTime? dateOfBirth,
            string gender,
            long guardianId,
            long campusId,
            long? excludedStudentId = null)
        {
            var normalizedDateOfBirth = dateOfBirth?.Date;

            var duplicatedStudent = await _studentRepo.Get()
                .FirstOrDefaultAsync(x =>
                    x.FullName.ToLower() == fullName.ToLower() &&
                    x.DateOfBirth.HasValue &&
                    normalizedDateOfBirth.HasValue &&
                    x.DateOfBirth.Value.Date == normalizedDateOfBirth.Value &&
                    x.Gender != null &&
                    x.Gender.ToLower() == gender.ToLower() &&
                    x.GuardianId == guardianId &&
                    x.CampusId == campusId &&
                    (!excludedStudentId.HasValue || x.Id != excludedStudentId.Value));

            if (duplicatedStudent != null)
                throw new Exception("Student đã tồn tại với đầy đủ thông tin trùng nhau");
        }

        private static StudentDto MapToStudentDto(Student student)
        {
            return new StudentDto
            {
                Id = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                AvatarUrl = student.AvatarUrl,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                GuardianId = student.GuardianId,
                GuardianName = student.Guardian?.FullName ?? string.Empty,
                CampusId = student.CampusId,
                CampusName = student.Campus?.Name ?? string.Empty,
                Status = student.Status.ToString()
            };
        }

        private static StudentDetailDto MapToStudentDetailDto(Student student)
        {
            return new StudentDetailDto
            {
                Id = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                AvatarUrl = student.AvatarUrl,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                GuardianId = student.GuardianId,
                Guardian = MapToGuardianDto(student.Guardian),
                CampusId = student.CampusId,
                CampusName = student.Campus?.Name ?? string.Empty,
                Status = student.Status.ToString()
            };
        }

        private static UserDto MapToGuardianDto(User? guardian)
        {
            if (guardian == null)
                return new UserDto();

            return new UserDto
            {
                Id = guardian.Id,
                Email = guardian.Email,
                FullName = guardian.FullName,
                Phone = guardian.Phone,
                DeviceToken = guardian.DeviceToken,
                DriverLicenseNumber = guardian.DriverLicenseNumber,
                DriverLicenseClass = guardian.DriverLicenseClass,
                DriverLicenseExpiryDate = guardian.DriverLicenseExpiryDate,
                RoleName = guardian.Role?.Name ?? string.Empty,
                Status = guardian.Status.ToString(),
                CreatedAt = guardian.CreatedAt
            };
        }
    }
}

