using BE_API.Dto.Attendance;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IRepository<Attendance> _attendanceRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Bus> _busRepo;
        private readonly IRepository<StudentBusAssignment> _assignmentRepo;

        public AttendanceService(
            IRepository<Attendance> attendanceRepo,
            IRepository<Student> studentRepo,
            IRepository<Bus> busRepo,
            IRepository<StudentBusAssignment> assignmentRepo)
        {
            _attendanceRepo = attendanceRepo;
            _studentRepo = studentRepo;
            _busRepo = busRepo;
            _assignmentRepo = assignmentRepo;
        }

        public async Task<PagedResult<AttendanceDto>> SearchAttendanceAsync(string? keyword, DateTime? date, int page, int pageSize)
        {
            var query = _attendanceRepo.Get();
               

            if (date.HasValue)
            {
                var selectedDate = date.Value.Date;
                query = query.Where(x => x.Date.Date == selectedDate).Include(x => x.Student)
                .Include(x => x.Bus);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Student.FullName.ToLower().Contains(keyword) ||
                    x.Bus.LicensePlate.ToLower().Contains(keyword)).Include(x => x.Student)
                .Include(x => x.Bus);
            }

            var totalItems = await query.CountAsync();

            var attendances = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AttendanceDto>
            {
                Items = attendances.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AttendanceDto> GetAttendanceByIdAsync(long id)
        {
            var attendance = await _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Attendance khong ton tai");

            return MapToDto(attendance);
        }

        public async Task<AttendanceDto> ManualCheckInAsync(AttendanceManualDto dto)
        {
            var (student, bus, attendanceDate, checkTime) = await ValidateManualAttendanceAsync(dto);

            var attendance = await _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.Date.Date == attendanceDate);

            if (attendance != null && attendance.CheckInTime.HasValue)
                throw new Exception("Hoc sinh da check in trong ngay nay");

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    StudentId = student.Id,
                    BusId = bus.Id,
                    Date = attendanceDate,
                    Method = AttendanceMethod.MANUAL,
                    Status = AttendanceStatus.PRESENT,
                    CheckInTime = checkTime
                };

                await _attendanceRepo.AddAsync(attendance);
                await _attendanceRepo.SaveChangesAsync();

                attendance.Student = student;
                attendance.Bus = bus;
                return MapToDto(attendance);
            }

            attendance.BusId = bus.Id;
            attendance.Bus = bus;
            attendance.CheckInTime = checkTime;
            attendance.Method = AttendanceMethod.MANUAL;
            attendance.Status = AttendanceStatus.PRESENT;

            _attendanceRepo.Update(attendance);
            await _attendanceRepo.SaveChangesAsync();

            return MapToDto(attendance);
        }

        public async Task<AttendanceDto> ManualCheckOutAsync(AttendanceManualDto dto)
        {
            var (_, bus, attendanceDate, checkTime) = await ValidateManualAttendanceAsync(dto);

            var attendance = await _attendanceRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.Date.Date == attendanceDate)
                ?? throw new Exception("Khong tim thay attendance de check out");

            if (!attendance.CheckInTime.HasValue)
                throw new Exception("Hoc sinh chua check in");

            if (attendance.CheckOutTime.HasValue)
                throw new Exception("Hoc sinh da check out trong ngay nay");

            attendance.BusId = bus.Id;
            attendance.Bus = bus;
            attendance.CheckOutTime = checkTime;
            attendance.Method = AttendanceMethod.MANUAL;
            attendance.Status = AttendanceStatus.PRESENT;

            _attendanceRepo.Update(attendance);
            await _attendanceRepo.SaveChangesAsync();

            return MapToDto(attendance);
        }

        public async Task DeleteAttendanceAsync(long id)
        {
            var attendance = await _attendanceRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Attendance khong ton tai");

            _attendanceRepo.Delete(attendance);
            await _attendanceRepo.SaveChangesAsync();
        }

        private async Task<(Student student, Bus bus, DateTime attendanceDate, TimeSpan checkTime)> ValidateManualAttendanceAsync(AttendanceManualDto dto)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == dto.StudentId)
                ?? throw new Exception("Student khong ton tai");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == dto.BusId)
                ?? throw new Exception("Bus khong ton tai");

            if (!bus.IsEnabled)
                throw new Exception("Bus da bi vo hieu hoa");

            var hasAssignment = await _assignmentRepo.Get()
                .AnyAsync(x => x.StudentId == dto.StudentId && x.BusId == dto.BusId);

            if (!hasAssignment)
                throw new Exception("Hoc sinh chua duoc gan vao bus nay");

            var attendanceDate = (dto.Date ?? DateTime.Now).Date;
            var checkTime = dto.Time ?? DateTime.Now.TimeOfDay;

            return (student, bus, attendanceDate, checkTime);
        }

        private static AttendanceDto MapToDto(Attendance attendance)
        {
            return new AttendanceDto
            {
                Id = attendance.Id,
                StudentId = attendance.StudentId,
                StudentName = attendance.Student.FullName,
                BusId = attendance.BusId,
                BusLicensePlate = attendance.Bus.LicensePlate,
                Date = attendance.Date,
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                Method = attendance.Method.ToString(),
                Status = attendance.Status.ToString()
            };
        }
    }
}
