using BE_API.Dto.StudentBusAssignment;
using BE_API.Entites;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class StudentBusAssignmentService : IStudentBusAssignmentService
    {
        private readonly IRepository<StudentBusAssignment> _assignmentRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Bus> _busRepo;
        private readonly IRepository<BusRoute> _routeRepo;
        private readonly IRepository<BusRouteStation> _routeStationRepo;
        private readonly IRepository<BusAssignment> _busAssignmentRepo;

        public StudentBusAssignmentService(
            IRepository<StudentBusAssignment> assignmentRepo,
            IRepository<Student> studentRepo,
            IRepository<User> userRepo,
            IRepository<Bus> busRepo,
            IRepository<BusRoute> routeRepo,
            IRepository<BusRouteStation> routeStationRepo,
            IRepository<BusAssignment> busAssignmentRepo)
        {
            _assignmentRepo = assignmentRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _busRepo = busRepo;
            _routeRepo = routeRepo;
            _routeStationRepo = routeStationRepo;
            _busAssignmentRepo = busAssignmentRepo;
        }

        public async Task<StudentBusAssignmentDto> CreateAsync(StudentBusAssignmentCreateDto dto)
        {
            var rideDate = dto.RideDate.Date;
            var student = await ValidateStudentAsync(dto.StudentId);
            var bus = await ValidateBusAsync(dto.BusId);
            var route = await ValidateRouteAsync(dto.RouteId);
            //await ValidateBusRoutePairAsync(dto.BusId, dto.RouteId, rideDate);
            var (pickupStation, dropOffStation) = await ValidateStationsAsync(dto.RouteId, dto.PickupStationId, dto.DropOffStationId);

            var existed = await _assignmentRepo.Get()
                .AnyAsync(x =>
                    x.StudentId == dto.StudentId &&
                    x.RideDate.HasValue &&
                    x.RideDate.Value.Date == rideDate);

            if (existed)
                throw new Exception("Hoc sinh da duoc set diem don tra trong ngay nay");

            var assignment = new StudentBusAssignment
            {
                StudentId = student.Id,
                BusId = bus.Id,
                RouteId = route.Id,
                RideDate = rideDate,
                PickupStationId = pickupStation.Id,
                DropOffStationId = dropOffStation.Id
            };

            await _assignmentRepo.AddAsync(assignment);
            await _assignmentRepo.SaveChangesAsync();

            return await GetByIdAsync(assignment.Id);
        }

        public async Task<StudentBusAssignmentDto> GetByIdAsync(long id)
        {
            var assignment = await GetAssignmentQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student bus assignment khong ton tai");

            return MapToDto(assignment);
        }

        public async Task<List<StudentBusAssignmentDto>> GetByStudentIdAsync(long studentId, DateTime? rideDate)
        {
            await ValidateStudentAsync(studentId);

            var query = GetAssignmentQueryable()
                .Where(x => x.StudentId == studentId);

            if (rideDate.HasValue)
            {
                var selectedDate = rideDate.Value.Date;
                query = query.Where(x => x.RideDate.HasValue && x.RideDate.Value.Date == selectedDate);
            }

            var assignments = await query
                .OrderByDescending(x => x.RideDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return assignments.Select(MapToDto).ToList();
        }

        public async Task<List<StudentBusAssignmentDto>> GetByGuardianIdAsync(long guardianId, DateTime? rideDate)
        {
            await ValidateGuardianAsync(guardianId);

            var query = GetAssignmentQueryable()
                .Where(x => x.Student.GuardianId == guardianId);

            if (rideDate.HasValue)
            {
                var selectedDate = rideDate.Value.Date;
                query = query.Where(x => x.RideDate.HasValue && x.RideDate.Value.Date == selectedDate);
            }

            var assignments = await query
                .OrderByDescending(x => x.RideDate)
                .ThenBy(x => x.Student.FullName)
                .ToListAsync();

            return assignments.Select(MapToDto).ToList();
        }

        public async Task<StudentBusAssignmentDto> UpdateAsync(long id, StudentBusAssignmentUpdateDto dto)
        {
            var assignment = await _assignmentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student bus assignment khong ton tai");

            var studentId = dto.StudentId ?? assignment.StudentId;
            var busId = dto.BusId ?? assignment.BusId;
            var routeId = dto.RouteId ?? assignment.RouteId;
            var rideDate = (dto.RideDate ?? assignment.RideDate ?? DateTime.Now).Date;
            var pickupStationId = dto.PickupStationId ?? assignment.PickupStationId
                ?? throw new Exception("PickupStationId khong duoc de trong");
            var dropOffStationId = dto.DropOffStationId ?? assignment.DropOffStationId
                ?? throw new Exception("DropOffStationId khong duoc de trong");

            await ValidateStudentAsync(studentId);
            await ValidateBusAsync(busId);
            await ValidateRouteAsync(routeId);
            //await ValidateBusRoutePairAsync(busId, routeId, rideDate);
            await ValidateStationsAsync(routeId, pickupStationId, dropOffStationId);

            var duplicated = await _assignmentRepo.Get()
                .AnyAsync(x =>
                    x.Id != id &&
                    x.StudentId == studentId &&
                    x.RideDate.HasValue &&
                    x.RideDate.Value.Date == rideDate);

            if (duplicated)
                throw new Exception("Hoc sinh da duoc set diem don tra trong ngay nay");

            assignment.StudentId = studentId;
            assignment.BusId = busId;
            assignment.RouteId = routeId;
            assignment.RideDate = rideDate;
            assignment.PickupStationId = pickupStationId;
            assignment.DropOffStationId = dropOffStationId;

            _assignmentRepo.Update(assignment);
            await _assignmentRepo.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(long id)
        {
            var assignment = await _assignmentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Student bus assignment khong ton tai");

            _assignmentRepo.Delete(assignment);
            await _assignmentRepo.SaveChangesAsync();
        }

        private IQueryable<StudentBusAssignment> GetAssignmentQueryable()
        {
            return _assignmentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Bus)
                .Include(x => x.Route)
                .Include(x => x.PickupStation)
                .Include(x => x.DropOffStation);
        }

        private async Task<Student> ValidateStudentAsync(long studentId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phai lon hon 0");

            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == studentId)
                ?? throw new Exception("Student khong ton tai");

            if (student.Status != AccountStatus.ACTIVE)
                throw new Exception("Student dang khong hoat dong");

            return student;
        }

        private async Task ValidateGuardianAsync(long guardianId)
        {
            if (guardianId <= 0)
                throw new Exception("GuardianId phai lon hon 0");

            var guardian = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == guardianId)
                ?? throw new Exception("Guardian khong ton tai");

            if (!string.Equals(guardian.Role.Name, "guardian", StringComparison.OrdinalIgnoreCase))
                throw new Exception("User duoc chon khong phai guardian");

            if (guardian.Status != AccountStatus.ACTIVE)
                throw new Exception("Guardian dang khong hoat dong");
        }

        private async Task<Bus> ValidateBusAsync(long busId)
        {
            if (busId <= 0)
                throw new Exception("BusId phai lon hon 0");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == busId)
                ?? throw new Exception("Bus khong ton tai");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bus dang khong hoat dong");

            return bus;
        }

        private async Task<BusRoute> ValidateRouteAsync(long routeId)
        {
            if (routeId <= 0)
                throw new Exception("RouteId phai lon hon 0");

            var route = await _routeRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == routeId)
                ?? throw new Exception("Bus route khong ton tai");

            if (!route.IsEnabled)
                throw new Exception("Bus route dang khong hoat dong");

            return route;
        }

        private async Task ValidateBusRoutePairAsync(long busId, long routeId, DateTime rideDate)
        {
            var exists = await _busAssignmentRepo.Get()
                .AnyAsync(x =>
                    x.BusId == busId &&
                    x.RouteId == routeId &&
                    (!x.ActiveDate.HasValue || x.ActiveDate.Value.Date == rideDate));

            if (!exists)
                throw new Exception("Bus nay chua duoc gan vao route da chon");
        }

        private async Task<(BusStation pickupStation, BusStation dropOffStation)> ValidateStationsAsync(
            long routeId,
            long pickupStationId,
            long dropOffStationId)
        {
            if (pickupStationId <= 0)
                throw new Exception("PickupStationId phai lon hon 0");

            if (dropOffStationId <= 0)
                throw new Exception("DropOffStationId phai lon hon 0");

            var routeStations = await _routeStationRepo.Get()
                .Where(x =>
                    x.RouteId == routeId &&
                    (x.StationId == pickupStationId || x.StationId == dropOffStationId))
                .Include(x => x.Station)
                .ToListAsync();

            var pickupStation = routeStations
                .Where(x => x.StationId == pickupStationId)
                .Select(x => x.Station)
                .FirstOrDefault()
                ?? throw new Exception("Diem don khong thuoc route da chon");

            var dropOffStation = routeStations
                .Where(x => x.StationId == dropOffStationId)
                .Select(x => x.Station)
                .FirstOrDefault()
                ?? throw new Exception("Diem tra khong thuoc route da chon");

            if (!pickupStation.IsEnabled)
                throw new Exception($"Bus station '{pickupStation.Name}' dang khong hoat dong");

            if (!dropOffStation.IsEnabled)
                throw new Exception($"Bus station '{dropOffStation.Name}' dang khong hoat dong");

            return (pickupStation, dropOffStation);
        }

        private static StudentBusAssignmentDto MapToDto(StudentBusAssignment assignment)
        {
            return new StudentBusAssignmentDto
            {
                Id = assignment.Id,
                StudentId = assignment.StudentId,
                StudentName = assignment.Student.FullName,
                GuardianId = assignment.Student.GuardianId,
                BusId = assignment.BusId,
                BusLicensePlate = assignment.Bus.LicensePlate,
                RouteId = assignment.RouteId,
                RouteName = assignment.Route.Name,
                RideDate = assignment.RideDate,
                PickupStationId = assignment.PickupStationId,
                PickupStationName = assignment.PickupStation?.Name,
                DropOffStationId = assignment.DropOffStationId,
                DropOffStationName = assignment.DropOffStation?.Name
            };
        }
    }
}
