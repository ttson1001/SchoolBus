using BE_API.Dto.Common;
using BE_API.Dto.Student;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class StudentService : IStudentService
    {
        private readonly IRepository<Student> _studentRepo;

        public StudentService(IRepository<Student> studentRepo)
        {
            _studentRepo = studentRepo;
        }

        public async Task<PagedResult<StudentDto>> SearchStudentAsync(string? keyword, int page, int pageSize)
        {
            IQueryable<Student> query = _studentRepo.Get()
                .Include(x => x.Guardian)
                .Include(x => x.Campus);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.FullName.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();

            var students = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = students.Select(x => new StudentDto
            {
                Id = x.Id,
                FullName = x.FullName,
                DateOfBirth = x.DateOfBirth,
                Gender = x.Gender,
                GuardianId = x.GuardianId,
                GuardianName = x.Guardian.FullName,
                CampusId = x.CampusId,
                CampusName = x.Campus.Name,
                Status = x.Status.ToString()
            }).ToList();

            return new PagedResult<StudentDto>
            {
                Items = items,
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

            return new StudentDto
            {
                Id = student.Id,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                GuardianId = student.GuardianId,
                GuardianName = student.Guardian.FullName,
                CampusId = student.CampusId,
                CampusName = student.Campus.Name,
                Status = student.Status.ToString()
            };
        }

        public async Task CreateStudentAsync(StudentCreateDto dto)
        {
            var student = new Student
            {
                FullName = dto.FullName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                GuardianId = dto.GuardianId,
                CampusId = dto.CampusId,
                Status = AccountStatus.ACTIVE
            };

            await _studentRepo.AddAsync(student);
        }

        public async Task<StudentDto> UpdateStudentAsync(long id, StudentUpdateDto dto)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                student.FullName = dto.FullName;

            if (dto.DateOfBirth.HasValue)
                student.DateOfBirth = dto.DateOfBirth;

            if (!string.IsNullOrWhiteSpace(dto.Gender))
                student.Gender = dto.Gender;

            if (dto.GuardianId.HasValue)
                student.GuardianId = dto.GuardianId.Value;

            if (dto.CampusId.HasValue)
                student.CampusId = dto.CampusId.Value;

            _studentRepo.Update(student);

            return new StudentDto
            {
                Id = student.Id,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                GuardianId = student.GuardianId,
                CampusId = student.CampusId,
                Status = student.Status.ToString()
            };
        }

        public async Task DeleteStudentAsync(long id)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student không tồn tại");

            _studentRepo.Delete(student);
        }
    }
}
