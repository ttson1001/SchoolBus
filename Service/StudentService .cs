using BE_API.Dto.Common;
using BE_API.Dto.Student;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BE_API.Service
{
    public class StudentService : IStudentService
    {
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Campus> _campusRepo;

        public StudentService(
            IRepository<Student> studentRepo,
            IRepository<User> userRepo,
            IRepository<Campus> campusRepo)
        {
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _campusRepo = campusRepo;
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
                    throw new Exception($"Status '{status}' khong hop le.");

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
                throw new Exception("PhoneNumber khong duoc de trong");

            var normalizedPhoneNumber = phoneNumber.Trim();

            var guardian = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.Phone != null &&
                    x.Phone.Trim().ToLower() == normalizedPhoneNumber.ToLower())
                ?? throw new Exception("Khong tim thay guardian voi so dien thoai da cung cap");

            if (!string.Equals(guardian.Role.Name, "guardian", StringComparison.OrdinalIgnoreCase))
                throw new Exception("So dien thoai nay khong thuoc tai khoan guardian");

            if (guardian.Status != AccountStatus.ACTIVE)
                throw new Exception("Guardian dang khong hoat dong");

            var students = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus)
                .Where(x => x.GuardianId == guardian.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return students.Select(MapToStudentDto).ToList();
        }

        public async Task CreateStudentAsync(StudentCreateDto dto)
        {
            var normalizedFullName = NormalizeRequiredFullName(dto.FullName);
            var avatarUrl = NormalizeAvatarUrl(dto.AvatarUrl);
            var dateOfBirth = ValidateDateOfBirth(dto.DateOfBirth);
            var gender = NormalizeGender(dto.Gender);

            await ValidateGuardianAsync(dto.GuardianId);
            await ValidateCampusAsync(dto.CampusId);
            await EnsureStudentNotDuplicatedAsync(
                normalizedFullName,
                dateOfBirth,
                gender,
                dto.GuardianId,
                dto.CampusId);

            var student = new Student
            {
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

        private static string NormalizeRequiredFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new Exception("FullName không được để trống");

            var normalizedFullName = fullName.Trim();

            if (normalizedFullName.Length > 100)
                throw new Exception("FullName không được vượt quá 100 ký tự");

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

        private static DateTime ValidateDateOfBirth(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
                throw new Exception("DateOfBirth không được để trống");

            var normalizedDate = dateOfBirth.Value.Date;
            var today = DateTime.UtcNow.Date;

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
    }
}
