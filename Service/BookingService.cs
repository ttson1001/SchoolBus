using BE_API.Common;
using BE_API.Configuration;
using BE_API.Entites;
using BE_API.Dto.Booking;
using BE_API.Dto.Common;
using BE_API.Entites.Enums;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BE_API.Service
{
    public class BookingService : IBookingService
    {
        private const double MaxPickupDistanceKm = 4d;

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "PENDING",
            "CONFIRMED",
            "CANCELLED"
        };

        private readonly IRepository<Booking> _bookingRepo;
        private readonly IRepository<BusRun> _busRunRepo;
        private readonly IRepository<BusRunStudent> _busRunStudentRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<BusRoute> _routeRepo;
        private readonly IRepository<BusRouteStation> _routeStationRepo;
        private readonly IRepository<Bus> _busRepo;
        private readonly IRepository<Attendance> _attendanceRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IAppTime _appTime;
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly BookingSlotSettings _bookingSlotSettings;

        public BookingService(
            IRepository<Booking> bookingRepo,
            IRepository<BusRun> busRunRepo,
            IRepository<BusRunStudent> busRunStudentRepo,
            IRepository<Student> studentRepo,
            IRepository<User> userRepo,
            IRepository<BusRoute> routeRepo,
            IRepository<BusRouteStation> routeStationRepo,
            IRepository<Bus> busRepo,
            IRepository<Attendance> attendanceRepo,
            IRepository<Notification> notificationRepo,
            IAppTime appTime,
            IFirebaseNotificationService firebaseNotificationService,
            IOptions<BookingSlotSettings> bookingSlotOptions)
        {
            _bookingRepo = bookingRepo;
            _busRunRepo = busRunRepo;
            _busRunStudentRepo = busRunStudentRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _routeRepo = routeRepo;
            _routeStationRepo = routeStationRepo;
            _busRepo = busRepo;
            _attendanceRepo = attendanceRepo;
            _notificationRepo = notificationRepo;
            _appTime = appTime;
            _firebaseNotificationService = firebaseNotificationService;
            _bookingSlotSettings = bookingSlotOptions.Value;
        }

        public async Task<PagedResult<BookingDto>> SearchAsync(
            long? studentId,
            long? routeId,
            DateTime? serviceDate,
            string? status,
            int page,
            int pageSize)
        {
            var query = GetQueryable();

            if (studentId.HasValue)
                query = query.Where(x => x.StudentId == studentId.Value);

            if (routeId.HasValue)
                query = query.Where(x => x.RouteId == routeId.Value);

            if (serviceDate.HasValue)
            {
                var selectedDate = serviceDate.Value.Date;
                query = query.Where(x => x.ServiceDate.Date == selectedDate);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = NormalizeStatus(status);
                query = query.Where(x => x.Status == normalizedStatus);
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.ServiceDate)
                .ThenBy(x => x.StartTime)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BookingDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<BookingDto> GetByIdAsync(long id)
        {
            var booking = await GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Booking không tồn tại");

            return MapToDto(booking);
        }

        public async Task<List<BusRunDto>> GetBusRunsAsync(
            DateTime serviceDate,
            long? routeId,
            long? busId,
            long? driverId,
            long? teacherId)
        {
            var normalizedServiceDate = serviceDate.Date;
            var query = _busRunRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.Bus)
                .Include(x => x.Driver)
                .ThenInclude(x => x.Role)
                .Include(x => x.Teacher)
                .ThenInclude(x => x.Role)
                .Where(x => x.ServiceDate.Date == normalizedServiceDate);

            if (routeId.HasValue)
                query = query.Where(x => x.RouteId == routeId.Value);

            if (busId.HasValue)
                query = query.Where(x => x.BusId == busId.Value);

            if (driverId.HasValue)
                query = query.Where(x => x.DriverId == driverId.Value);

            if (teacherId.HasValue)
                query = query.Where(x => x.TeacherId == teacherId.Value);

            var runs = await query
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.Route.Name)
                .ThenBy(x => x.RunOrder)
                .ToListAsync();

            var runIds = runs.Select(x => x.Id).ToList();
            var routeIds = runs.Select(x => x.RouteId).Distinct().ToList();
            var startTimes = runs.Select(x => x.StartTime).Distinct().ToList();
            var runStudents = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Where(x =>
                    x.BusRun.ServiceDate.Date == normalizedServiceDate &&
                    routeIds.Contains(x.BusRun.RouteId) &&
                    startTimes.Contains(x.BusRun.StartTime))
                .ToListAsync();

            var studentIds = runStudents
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var attendanceRows = await _attendanceRepo.Get()
                .Include(x => x.Bus)
                .Where(x =>
                    studentIds.Contains(x.StudentId) &&
                    x.Date.Date == normalizedServiceDate)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return runs
                .Select(x => MapRunToDto(
                    x,
                    runStudents,
                    attendanceRows))
                .ToList();
        }

        public async Task<List<GuardianBusRunWithTomorrowBookingDto>> GetTodayBusRunsByGuardianAsync(long guardianId, DateTime? serviceDate)
        {
            await ValidateGuardianAsync(guardianId);

            var selectedDate = serviceDate?.Date ?? _appTime.TodayDate;
            var tomorrowDate = selectedDate.AddDays(1);

            var assignments = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .ThenInclude(x => x.Guardian)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Include(x => x.BusRun)
                .ThenInclude(x => x.Route)
                .Include(x => x.BusRun)
                .ThenInclude(x => x.Bus)
                .Include(x => x.BusRun)
                .ThenInclude(x => x.Driver)
                .Include(x => x.BusRun)
                .ThenInclude(x => x.Teacher)
                .Where(x =>
                    x.Student.GuardianId == guardianId &&
                    x.Booking.ServiceDate.Date == selectedDate)
                .OrderBy(x => x.Booking.StartTime)
                .ThenBy(x => x.BusRun.RunOrder)
                .ThenBy(x => x.Student.StudentCode)
                .ToListAsync();

            if (!assignments.Any())
                return new List<GuardianBusRunWithTomorrowBookingDto>();

            var studentIds = assignments
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var attendanceRows = await _attendanceRepo.Get()
                .Include(x => x.Bus)
                .Where(x =>
                    studentIds.Contains(x.StudentId) &&
                    x.Date.Date == selectedDate)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var tomorrowBookings = await _bookingRepo.Get()
                .Include(x => x.Student)
                .ThenInclude(x => x.Guardian)
                .Include(x => x.Route)
                .Include(x => x.Station)
                .Where(x =>
                    studentIds.Contains(x.StudentId) &&
                    x.ServiceDate.Date == tomorrowDate)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return assignments
                .GroupBy(x => x.BookingId)
                .Select(group =>
                {
                    var assignment = group
                        .OrderBy(x => x.Id)
                        .First();

                    var assignedRun = assignment.BusRun;

                    var hasCheckedInOnThisBus = attendanceRows.Any(a =>
                        a.StudentId == assignment.StudentId &&
                        a.BusId == assignedRun.BusId &&
                        a.CheckInTime.HasValue);

                    var isCurrentlyOnThisBus = attendanceRows.Any(a =>
                        a.StudentId == assignment.StudentId &&
                        a.BusId == assignedRun.BusId &&
                        a.CheckInTime.HasValue &&
                        !a.CheckOutTime.HasValue);

                    var activeOnOtherBus = attendanceRows.FirstOrDefault(a =>
                        a.StudentId == assignment.StudentId &&
                        a.BusId != assignedRun.BusId &&
                        a.CheckInTime.HasValue &&
                        !a.CheckOutTime.HasValue);

                    var tomorrowBooking = tomorrowBookings
                        .FirstOrDefault(x => x.StudentId == assignment.StudentId);

                    return new GuardianBusRunWithTomorrowBookingDto
                    {
                        TodayBusRun = new GuardianTodayBusRunDto
                        {
                            BookingId = assignment.BookingId,
                            StudentId = assignment.StudentId,
                            StudentCode = assignment.Student.StudentCode,
                            StudentName = assignment.Student.FullName,
                            StudentAvatarUrl = assignment.Student.AvatarUrl,
                            RouteId = assignedRun.RouteId,
                            RouteName = assignedRun.Route.Name,
                            ServiceDate = assignedRun.ServiceDate,
                            StartTime = assignedRun.StartTime,
                            BusRunId = assignedRun.Id,
                            BusId = assignedRun.BusId,
                            BusLabel = !string.IsNullOrWhiteSpace(assignedRun.Bus.BusNumber)
                                ? assignedRun.Bus.BusNumber
                                : assignedRun.Bus.LicensePlate,
                            DriverId = assignedRun.DriverId,
                            DriverName = assignedRun.Driver?.FullName ?? assignedRun.Driver?.Email,
                            TeacherId = assignedRun.TeacherId,
                            TeacherName = assignedRun.Teacher?.FullName ?? assignedRun.Teacher?.Email,
                            RunOrder = assignedRun.RunOrder,
                            RunStatus = assignedRun.Status,
                            TodayStatus = ResolveGuardianTodayStatus(
                                hasCheckedInOnThisBus,
                                isCurrentlyOnThisBus,
                                activeOnOtherBus != null),
                            StationId = assignment.Booking.StationId,
                            StationName = assignment.Booking.Station?.Name ?? string.Empty,
                            PickupAddress = assignment.Booking.PickupAddress,
                            HasCheckedInOnThisBus = hasCheckedInOnThisBus,
                            IsCurrentlyOnThisBus = isCurrentlyOnThisBus,
                            CurrentBusId = activeOnOtherBus?.BusId,
                            CurrentBusLabel = activeOnOtherBus?.Bus != null
                                ? (!string.IsNullOrWhiteSpace(activeOnOtherBus.Bus.BusNumber)
                                    ? activeOnOtherBus.Bus.BusNumber
                                    : activeOnOtherBus.Bus.LicensePlate)
                                : null,
                            IsOnDifferentBusThanAssigned = activeOnOtherBus != null
                        },
                        BookingTomorrow = tomorrowBooking == null
                            ? null
                            : new BookingDto
                            {
                                Id = tomorrowBooking.Id,
                                StudentId = tomorrowBooking.StudentId,
                                StudentCode = tomorrowBooking.Student.StudentCode,
                                StudentName = tomorrowBooking.Student.FullName,
                                GuardianId = tomorrowBooking.Student.GuardianId,
                                GuardianName = tomorrowBooking.Student.Guardian?.FullName ?? string.Empty,
                                RouteId = tomorrowBooking.RouteId,
                                RouteName = tomorrowBooking.Route.Name,
                                ServiceDate = tomorrowBooking.ServiceDate,
                                StartTime = tomorrowBooking.StartTime,
                                StationId = tomorrowBooking.StationId,
                                StationName = tomorrowBooking.Station?.Name ?? string.Empty,
                                StationAddress = tomorrowBooking.Station?.Address,
                                PickupAddress = tomorrowBooking.PickupAddress,
                                Latitude = tomorrowBooking.Latitude,
                                Longitude = tomorrowBooking.Longitude,
                                Status = tomorrowBooking.Status,
                                Note = tomorrowBooking.Note,
                                CreatedAt = tomorrowBooking.CreatedAt
                            }
                    };
                })
                .OrderBy(x => x.TodayBusRun.StartTime)
                .ThenBy(x => x.TodayBusRun.RunOrder)
                .ThenBy(x => x.TodayBusRun.StudentCode)
                .ThenBy(x => x.TodayBusRun.StudentName)
                .ToList();
        }

        public async Task<BusRunDto> AssignBusRunStaffAsync(long busRunId, BusRunAssignStaffDto dto)
        {
            if (!dto.DriverId.HasValue && !dto.TeacherId.HasValue)
                throw new Exception("Phai chon it nhat driverId hoac teacherId");

            var busRun = await _busRunRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.Bus)
                .Include(x => x.Driver)
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.Id == busRunId)
                ?? throw new Exception("Bus run không tồn tại");

            var driverId = dto.DriverId ?? busRun.DriverId;
            var teacherId = dto.TeacherId ?? busRun.TeacherId;

            User? driver = null;
            User? teacher = null;

            if (driverId.HasValue)
                driver = await ValidateUserByRoleAsync(driverId.Value, "driver");

            if (teacherId.HasValue)
                teacher = await ValidateUserByRoleAsync(teacherId.Value, "teacher");

            if (driver != null && teacher != null && driver.Id == teacher.Id)
                throw new Exception("Driver và teacher không được là cùng một người");

            await EnsureBusRunStaffAvailabilityAsync(busRun, driver?.Id, teacher?.Id);

            busRun.DriverId = driver?.Id;
            busRun.TeacherId = teacher?.Id;

            _busRunRepo.Update(busRun);
            await _busRunRepo.SaveChangesAsync();

            return await GetBusRunByIdAsync(busRun.Id);
        }

        public async Task<BookingDto> CreateAsync(BookingCreateDto dto)
        {
            var student = await ValidateStudentAsync(dto.StudentId);
            var route = await ValidateRouteAsync(dto.RouteId);
            var serviceDate = NormalizeBookingServiceDate(dto.ServiceDate, dto.StartTime);
            ValidateBookingSlot(dto.StartTime);
            var station = await ResolveStationAsync(
                route.Id,
                dto.StationId,
                dto.Latitude,
                dto.Longitude);

            await EnsureBookingNotDuplicatedAsync(student.Id, route.Id, serviceDate, dto.StartTime, null);

            var booking = new Booking
            {
                StudentId = student.Id,
                RouteId = route.Id,
                ServiceDate = serviceDate,
                StartTime = dto.StartTime,
                StationId = station.Id,
                PickupAddress = NormalizeOptional(dto.PickupAddress),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Status = "PENDING",
                Note = NormalizeOptional(dto.Note),
                CreatedAt = DateTime.UtcNow
            };

            await _bookingRepo.AddAsync(booking);
            await _bookingRepo.SaveChangesAsync();

            return await GetByIdAsync(booking.Id);
        }

        public async Task<BookingDto> UpdateAsync(long id, BookingUpdateDto dto)
        {
            var booking = await _bookingRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Booking không tồn tại");

            var studentId = dto.StudentId ?? booking.StudentId;
            var routeId = dto.RouteId ?? booking.RouteId;
            var startTime = dto.StartTime ?? booking.StartTime;
            var serviceDate = NormalizeBookingServiceDate(dto.ServiceDate ?? booking.ServiceDate, startTime);
            var status = dto.Status != null ? NormalizeStatus(dto.Status) : booking.Status;
            var latitude = dto.Latitude ?? booking.Latitude;
            var longitude = dto.Longitude ?? booking.Longitude;

            await ValidateStudentAsync(studentId);
            var route = await ValidateRouteAsync(routeId);
            ValidateBookingSlot(startTime);
            var station = await ResolveStationAsync(
                route.Id,
                dto.StationId ?? booking.StationId,
                latitude,
                longitude);

            await EnsureBookingNotDuplicatedAsync(studentId, routeId, serviceDate, startTime, id);

            booking.StudentId = studentId;
            booking.RouteId = routeId;
            booking.ServiceDate = serviceDate;
            booking.StartTime = startTime;
            booking.StationId = station.Id;
            booking.PickupAddress = dto.PickupAddress != null
                ? NormalizeOptional(dto.PickupAddress)
                : booking.PickupAddress;
            booking.Latitude = latitude;
            booking.Longitude = longitude;
            booking.Status = status;
            booking.Note = dto.Note != null ? NormalizeOptional(dto.Note) : booking.Note;

            _bookingRepo.Update(booking);
            await _bookingRepo.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<List<BusRunDto>> AutoAssignBusRunsAsync(AutoAssignBookingRequestDto dto)
        {
            var route = await ValidateRouteAsync(dto.RouteId);
            var serviceDate = NormalizeAssignmentServiceDate(dto.ServiceDate);

            var bookings = await _bookingRepo.Get()
                .Include(x => x.Student)
                .ThenInclude(s => s.Guardian)
                .Include(x => x.Station)
                .Where(x =>
                    x.RouteId == route.Id &&
                    x.ServiceDate.Date == serviceDate.Date &&
                    x.StartTime == dto.StartTime &&
                    x.Status != "CANCELLED")
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            if (!bookings.Any())
                throw new Exception("Không có booking nào để chia xe");

            var existingRuns = await _busRunRepo.Get()
                .Where(x =>
                    x.RouteId == route.Id &&
                    x.ServiceDate.Date == serviceDate.Date &&
                    x.StartTime == dto.StartTime)
                .ToListAsync();

            if (existingRuns.Any())
            {
                var existingRunIds = existingRuns.Select(x => x.Id).ToList();
                var existingRunStudents = await _busRunStudentRepo.Get()
                    .Where(x => existingRunIds.Contains(x.BusRunId))
                    .ToListAsync();

                if (existingRunStudents.Any())
                {
                    _busRunStudentRepo.DeleteRange(existingRunStudents);
                    await _busRunStudentRepo.SaveChangesAsync();
                }

                _busRunRepo.DeleteRange(existingRuns);
                await _busRunRepo.SaveChangesAsync();
            }

            var minSoft = _bookingSlotSettings.SoftSlotMinStudents;
            if (minSoft > 0 &&
                !IsHardSlot(dto.StartTime) &&
                bookings.Count < minSoft)
            {
                await CancelSoftSlotBookingsAndNotifyAsync(bookings, route, serviceDate, dto.StartTime);
                throw new SoftSlotInsufficientStudentsException(
                    $"Không đủ học sinh để tạo chuyến xe (khung giờ mềm, cần tối thiểu {minSoft} học sinh). " +
                    $"Da huy {bookings.Count} booking va thong bao phu huynh. Ngay {serviceDate:dd/MM/yyyy}, {FormatSlotTime(dto.StartTime)}.");
            }

            var loads = BuildPrimaryBusLoads(bookings.Count);
            var availableBuses = await GetAvailableBusesAsync(serviceDate, dto.StartTime);
            var busPlan = SelectBusesForAssignment(loads, availableBuses);
            var staffPlan = await BuildStaffAssignmentPlanAsync(serviceDate, dto.StartTime, loads.Count + 1);
            var routeStations = await _routeStationRepo.Get()
                .Where(x => x.RouteId == route.Id)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();
            var bookingAssignments = BuildBalancedAssignmentsByPickup(routeStations, bookings, loads);

            foreach (var booking in bookings)
                booking.Status = "CONFIRMED";

            await _bookingRepo.SaveChangesAsync();

            var createdRuns = new List<BusRun>();
            for (var i = 0; i < loads.Count; i++)
            {
                var load = loads[i];
                var busSelection = busPlan.PrimaryBuses[i];

                var busRun = new BusRun
                {
                    RouteId = route.Id,
                    ServiceDate = serviceDate,
                    StartTime = dto.StartTime,
                    BusId = busSelection.bus.Id,
                    DriverId = staffPlan.DriverIds[i],
                    TeacherId = staffPlan.TeacherIds[i],
                    SeatCapacity = busSelection.bus.Capacity,
                    UsableCapacity = busSelection.usableCapacity,
                    AssignedStudentCount = load,
                    RunOrder = i + 1,
                    Status = "ASSIGNED",
                    CreatedAt = DateTime.UtcNow
                };

                await _busRunRepo.AddAsync(busRun);
                await _busRunRepo.SaveChangesAsync();
                createdRuns.Add(busRun);

                var runStudents = bookingAssignments[i]
                    .Select(booking => new BusRunStudent
                    {
                        BusRunId = busRun.Id,
                        BookingId = booking.Id,
                        StudentId = booking.StudentId
                    })
                    .ToList();

                await _busRunStudentRepo.AddRangeAsync(runStudents);
                await _busRunStudentRepo.SaveChangesAsync();
            }

            var backupRun = new BusRun
            {
                RouteId = route.Id,
                ServiceDate = serviceDate,
                StartTime = dto.StartTime,
                BusId = busPlan.BackupBus.Id,
                DriverId = staffPlan.DriverIds[loads.Count],
                TeacherId = staffPlan.TeacherIds[loads.Count],
                SeatCapacity = busPlan.BackupBus.Capacity,
                UsableCapacity = 15,
                AssignedStudentCount = 0,
                RunOrder = loads.Count + 1,
                Status = "BACKUP",
                CreatedAt = DateTime.UtcNow
            };

            await _busRunRepo.AddAsync(backupRun);
            await _busRunRepo.SaveChangesAsync();
            createdRuns.Add(backupRun);

            var runIds = createdRuns.Select(x => x.Id).ToList();
            var runs = await _busRunRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.Bus)
                .Include(x => x.Driver)
                .ThenInclude(x => x.Role)
                .Include(x => x.Teacher)
                .ThenInclude(x => x.Role)
                .Where(x => runIds.Contains(x.Id))
                .OrderBy(x => x.RunOrder)
                .ToListAsync();

            var runStudentsResult = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Where(x => runIds.Contains(x.BusRunId))
                .ToListAsync();

            var studentIds = runStudentsResult
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var attendanceRows = await _attendanceRepo.Get()
                .Include(x => x.Bus)
                .Where(x =>
                    studentIds.Contains(x.StudentId) &&
                    x.Date.Date == serviceDate.Date)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return runs
                .Select(run => MapRunToDto(
                    run,
                    runStudentsResult,
                    attendanceRows))
                .ToList();
        }

        public async Task<List<BusRunDto>> AutoAssignBusRunsByDateAsync(DateTime serviceDate)
        {
            var normalizedServiceDate = NormalizeAssignmentServiceDate(serviceDate);

            var groups = await _bookingRepo.Get()
                .Where(x =>
                    x.ServiceDate.Date == normalizedServiceDate.Date &&
                    x.Status != "CANCELLED")
                .Select(x => new
                {
                    x.RouteId,
                    x.ServiceDate,
                    x.StartTime
                })
                .Distinct()
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.RouteId)
                .ToListAsync();

            if (!groups.Any())
                throw new Exception("Không có booking nào để chia xe trong ngày đã chọn");

            var result = new List<BusRunDto>();

            foreach (var group in groups)
            {
                try
                {
                    var runs = await AutoAssignBusRunsAsync(new AutoAssignBookingRequestDto
                    {
                        RouteId = group.RouteId,
                        ServiceDate = group.ServiceDate,
                        StartTime = group.StartTime
                    });

                    result.AddRange(runs);
                }
                catch (SoftSlotInsufficientStudentsException)
                {
                }
            }

            return result
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.RouteId)
                .ThenBy(x => x.RunOrder)
                .ToList();
        }

        public async Task FinalizeSoftSlotBookingsForDateAsync(DateTime serviceDate)
        {
            var normalizedServiceDate = serviceDate.Date;
            if (normalizedServiceDate <= _appTime.TodayDate)
                throw new Exception("Chi co the xu ly soft booking cho ngay mai hoac xa hon");

            var softGroups = await _bookingRepo.Get()
                .Where(x =>
                    x.ServiceDate.Date == normalizedServiceDate &&
                    x.Status != "CANCELLED" &&
                    !IsHardSlot(x.StartTime))
                .Select(x => new
                {
                    x.RouteId,
                    x.StartTime
                })
                .Distinct()
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.RouteId)
                .ToListAsync();

            foreach (var group in softGroups)
            {
                var bookings = await _bookingRepo.Get()
                    .Include(x => x.Student)
                    .ThenInclude(x => x.Guardian)
                    .Include(x => x.Route)
                    .Where(x =>
                        x.ServiceDate.Date == normalizedServiceDate &&
                        x.RouteId == group.RouteId &&
                        x.StartTime == group.StartTime &&
                        x.Status != "CANCELLED")
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();

                if (!bookings.Any() || bookings.Count >= _bookingSlotSettings.SoftSlotMinStudents)
                    continue;

                await CancelSoftSlotBookingsAndNotifyAsync(
                    bookings,
                    bookings[0].Route,
                    normalizedServiceDate,
                    group.StartTime);
            }
        }

        public async Task DeleteAsync(long id)
        {
            var booking = await _bookingRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Booking không tồn tại");

            _bookingRepo.Delete(booking);
            await _bookingRepo.SaveChangesAsync();
        }

        public Task<BookingWeeklySlotsDto> GetWeeklyBookingSlotsAsync()
        {
            if (_bookingSlotSettings.StepMinutes <= 0)
                throw new Exception("Cấu hình BookingSlots:StepMinutes không hợp lệ");

            var cfgStart = _bookingSlotSettings.StartHour;
            var cfgEnd = _bookingSlotSettings.EndHour;

            var today = _appTime.TodayDate.Date;
            const int daysAfterToday = 7;
            var lastDay = today.AddDays(daysAfterToday);
            var slots = BuildMergedWeekSlots(cfgStart, cfgEnd);

            var viNames = new[] { "Chu nhat", "Thu hai", "Thu ba", "Thu tu", "Thu nam", "Thu sau", "Thu bay" };

            var days = new List<BookingDayTimeSlotsDto>(daysAfterToday + 1);
            for (var i = 0; i <= daysAfterToday; i++)
            {
                var date = today.AddDays(i);
                days.Add(new BookingDayTimeSlotsDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    DayOfWeek = (int)date.DayOfWeek,
                    DayName = viNames[(int)date.DayOfWeek],
                    Slots = slots
                });
            }

            return Task.FromResult(new BookingWeeklySlotsDto
            {
                WeekStartDate = today.ToString("yyyy-MM-dd"),
                WeekEndDate = lastDay.ToString("yyyy-MM-dd"),
                StartHour = cfgStart,
                EndHour = cfgEnd,
                Days = days
            });
        }

        private IReadOnlyList<string> BuildSoftSlotStartTimes(int startHour, int endHour)
        {
            var startSlot = new TimeSpan(startHour, 0, 0);
            var endSlot = new TimeSpan(endHour, 0, 0);
            var step = _bookingSlotSettings.StepMinutes;
            var list = new List<string>();
            for (var t = startSlot; t <= endSlot; t = t.Add(TimeSpan.FromMinutes(step)))
                list.Add(FormatSlotTime(t));

            return list;
        }

        private IReadOnlyList<BookingWeekSlotItemDto> BuildMergedWeekSlots(int windowStartHour, int windowEndHour)
        {
            var hardSet = new HashSet<string>(
                BuildHardSlotStartTimesValidated(windowStartHour, windowEndHour),
                StringComparer.Ordinal);

            var list = new List<BookingWeekSlotItemDto>();
            foreach (var startTime in BuildSoftSlotStartTimes(windowStartHour, windowEndHour))
            {
                list.Add(new BookingWeekSlotItemDto
                {
                    StartTime = startTime,
                    Kind = hardSet.Contains(startTime) ? "hard" : "soft"
                });
            }

            return list;
        }

        private IReadOnlyList<string> BuildHardSlotStartTimesValidated(int windowStartHour, int windowEndHour)
        {
            var raw = _bookingSlotSettings.HardSlotTimes;
            if (raw == null || raw.Count == 0)
                return Array.Empty<string>();

            var windowLo = new TimeSpan(windowStartHour, 0, 0);
            var windowHi = new TimeSpan(windowEndHour, 0, 0);

            var parsed = new List<TimeSpan>();
            foreach (var s in raw)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;
                if (!TimeSpan.TryParse(s.Trim(), out var ts))
                    throw new Exception($"Cấu hình BookingSlots:HardSlotTimes có giá trị không hợp lệ: '{s}'");
                ValidateBookingSlot(ts);
                if (ts < windowLo || ts > windowHi)
                    continue;
                parsed.Add(ts);
            }

            return parsed
                .Distinct()
                .OrderBy(x => x.Ticks)
                .Select(FormatSlotTime)
                .ToList();
        }

        private static string FormatSlotTime(TimeSpan t) =>
            $"{t.Hours:D2}:{t.Minutes:D2}";

        private bool IsHardSlot(TimeSpan startTime)
        {
            var hardLabels = BuildHardSlotStartTimesValidated(
                _bookingSlotSettings.StartHour,
                _bookingSlotSettings.EndHour);
            return new HashSet<string>(hardLabels, StringComparer.Ordinal).Contains(FormatSlotTime(startTime));
        }

        private async Task CancelSoftSlotBookingsAndNotifyAsync(
            List<Booking> bookings,
            BusRoute route,
            DateTime serviceDate,
            TimeSpan startTime)
        {
            foreach (var b in bookings)
                b.Status = "CANCELLED";

            await _bookingRepo.SaveChangesAsync();

            var timeLabel = FormatSlotTime(startTime);
            var dateLabel = serviceDate.ToString("dd/MM/yyyy");
            const string notifType = "BOOKING_SOFT_INSUFFICIENT";

            foreach (var group in bookings.GroupBy(x => x.Student.GuardianId))
            {
                var guardian = group.First().Student.Guardian;
                var names = string.Join(", ", group.Select(x => x.Student.FullName).Distinct());
                var message =
                    $"Không đủ học sinh để mở chuyến xe bus ngày {dateLabel}, khung giờ {timeLabel}, tuyến {route.Name}. " +
                    $"Booking của học sinh: {names}. Trạng thái đã chuyển thành hủy.";

                var notification = new Notification
                {
                    UserId = guardian.Id,
                    Type = notifType,
                    Message = message,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepo.AddAsync(notification);
                await _notificationRepo.SaveChangesAsync();

                await _firebaseNotificationService.SendAsync(
                    guardian.DeviceToken,
                    "Hủy chuyến xe - không đủ học sinh",
                    message,
                    new Dictionary<string, string>
                    {
                        ["type"] = notifType,
                        ["routeId"] = route.Id.ToString(),
                        ["serviceDate"] = serviceDate.ToString("yyyy-MM-dd"),
                        ["startTime"] = timeLabel
                    });
            }
        }

        private IQueryable<Booking> GetQueryable()
        {
            return _bookingRepo.Get()
                .Include(x => x.Student)
                .ThenInclude(x => x.Guardian)
                .Include(x => x.Route)
                .Include(x => x.Station);
        }

        private async Task<Student> ValidateStudentAsync(long studentId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            var student = await _studentRepo.Get()
                .Include(x => x.Guardian)
                .FirstOrDefaultAsync(x => x.Id == studentId)
                ?? throw new Exception("Student không tồn tại");

            if (student.Status != AccountStatus.ACTIVE)
                throw new Exception("Student đang không hoạt động");

            return student;
        }

        private async Task<BusRoute> ValidateRouteAsync(long routeId)
        {
            if (routeId <= 0)
                throw new Exception("RouteId phải lớn hơn 0");

            var route = await _routeRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == routeId)
                ?? throw new Exception("Bus route không tồn tại");

            if (!route.IsEnabled)
                throw new Exception("Bus route đang không hoạt động");

            return route;
        }

        private DateTime NormalizeBookingServiceDate(DateTime serviceDate, TimeSpan startTime)
        {
            var normalizedServiceDate = serviceDate.Date;
            var today = _appTime.TodayDate;

            if (IsHardSlot(startTime))
            {
                if (normalizedServiceDate < today.AddDays(Math.Max(1, _bookingSlotSettings.HardSlotAdvanceDays)))
                    throw new Exception("Khung giờ cứng phải được đặt trước ít nhất 1 ngày");

                var tomorrow = today.AddDays(1);
                var cutoffTime = new TimeSpan(20, 0, 0);
                if (normalizedServiceDate == tomorrow && _appTime.GetTimeOfDay() >= cutoffTime)
                    throw new Exception("Sau 20:00 không thể booking cho ngày mai với khung giờ cứng");
            }
            else
            {
                if (normalizedServiceDate < today.AddDays(Math.Max(2, _bookingSlotSettings.SoftSlotAdvanceDays)))
                    throw new Exception("Khung giờ mềm phải được đặt trước ít nhất 2 ngày");
            }

            return normalizedServiceDate;
        }

        private DateTime NormalizeAssignmentServiceDate(DateTime serviceDate)
        {
            var normalizedServiceDate = serviceDate.Date;
            if (normalizedServiceDate <= _appTime.TodayDate)
                throw new Exception("Chỉ có thể chia xe cho ngày mai hoặc xa hơn");

            return normalizedServiceDate;
        }

        private void ValidateBookingSlot(TimeSpan startTime)
        {
            if (_bookingSlotSettings.StepMinutes <= 0)
                throw new Exception("Cấu hình BookingSlots:StepMinutes không hợp lệ");

            var startSlot = new TimeSpan(_bookingSlotSettings.StartHour, 0, 0);
            var endSlot = new TimeSpan(_bookingSlotSettings.EndHour, 0, 0);

            if (startTime < startSlot || startTime > endSlot)
                throw new Exception($"Khung giờ booking chỉ được phép từ {startSlot:hh\\:mm} đến {endSlot:hh\\:mm}");

            var minutesFromStart = (int)(startTime - startSlot).TotalMinutes;
            if (minutesFromStart % _bookingSlotSettings.StepMinutes != 0)
                throw new Exception($"Khung giờ booking phải theo bước {_bookingSlotSettings.StepMinutes} phút");
        }

        private async Task<BusStation> ResolveStationAsync(
            long routeId,
            long? stationId,
            double? latitude,
            double? longitude)
        {
            var routeStations = await _routeStationRepo.Get()
                .Where(x => x.RouteId == routeId)
                .Include(x => x.Station)
                .ToListAsync();

            if (!routeStations.Any())
                throw new Exception("Route chưa có trạm để booking");

            var station = ResolveStation(routeStations, stationId, latitude, longitude, "chon");

            if (!station.IsEnabled)
                throw new Exception($"Bus station '{station.Name}' đang không hoạt động");

            // Neu nguoi dung truyen toa do diem don, bat buoc toa do nay khong duoc cach tram da chon qua 4km.
            if (latitude.HasValue && longitude.HasValue)
            {
                if (!station.Latitude.HasValue || !station.Longitude.HasValue)
                    throw new Exception($"Bus station '{station.Name}' chưa có tọa độ để tính khoảng cách");

                var distanceKm = CalculateDistanceKm(
                    latitude.Value,
                    longitude.Value,
                    station.Latitude.Value,
                    station.Longitude.Value);

                if (distanceKm > MaxPickupDistanceKm)
                    throw new Exception($"Diem don cach tram '{station.Name}' {distanceKm:F2}km, vuot qua gioi han {MaxPickupDistanceKm:0.#}km");
            }

            return station;
        }

        private async Task EnsureBookingNotDuplicatedAsync(
            long studentId,
            long routeId,
            DateTime serviceDate,
            TimeSpan startTime,
            long? excludedId)
        {
            var duplicated = await _bookingRepo.Get()
                .AnyAsync(x =>
                    x.StudentId == studentId &&
                    x.RouteId == routeId &&
                    x.ServiceDate.Date == serviceDate.Date &&
                    x.StartTime == startTime &&
                    (!excludedId.HasValue || x.Id != excludedId.Value));

            if (duplicated)
                throw new Exception("Booking đã tồn tại cho học sinh ở khung giờ này");
        }

        private async Task<List<Bus>> GetAvailableBusesAsync(DateTime serviceDate, TimeSpan startTime)
        {
            var usedBusIds = await _busRunRepo.Get()
                .Where(x => x.ServiceDate.Date == serviceDate.Date && x.StartTime == startTime)
                .Select(x => x.BusId)
                .ToListAsync();

            return await _busRepo.Get()
                .Where(x =>
                    x.Status == "ACTIVE" &&
                    !usedBusIds.Contains(x.Id) &&
                    x.Capacity >= 15)
                .OrderByDescending(x => x.Capacity)
                .ToListAsync();
        }

        private async Task<StaffAssignmentPlan> BuildStaffAssignmentPlanAsync(DateTime serviceDate, TimeSpan startTime, int requiredRunCount)
        {
            var driverIds = await GetRotatingUserIdsByRoleAsync("driver", serviceDate, startTime, requiredRunCount);
            var teacherIds = await GetRotatingUserIdsByRoleAsync("teacher", serviceDate, startTime, requiredRunCount);

            return new StaffAssignmentPlan
            {
                DriverIds = driverIds,
                TeacherIds = teacherIds
            };
        }

        private async Task<List<long?>> GetRotatingUserIdsByRoleAsync(
            string roleName,
            DateTime serviceDate,
            TimeSpan startTime,
            int requiredCount)
        {
            var normalizedRoleName = roleName.Trim().ToLowerInvariant();

            var candidates = await _userRepo.Get()
                .Include(x => x.Role)
                .Where(x =>
                    x.Status == AccountStatus.ACTIVE &&
                    x.Role.Name.ToLower() == normalizedRoleName)
                .ToListAsync();

            if (normalizedRoleName == "driver")
            {
                candidates = candidates
                    .Where(x =>
                        x.DriverLicenseExpiryDate == null ||
                        x.DriverLicenseExpiryDate.Value.Date > _appTime.TodayDate)
                    .ToList();
            }

            var usedUserIdsAtSameTime = normalizedRoleName == "driver"
                ? await _busRunRepo.Get()
                    .Where(x => x.ServiceDate.Date == serviceDate.Date && x.StartTime == startTime && x.DriverId.HasValue)
                    .Select(x => x.DriverId!.Value)
                    .Distinct()
                    .ToListAsync()
                : await _busRunRepo.Get()
                    .Where(x => x.ServiceDate.Date == serviceDate.Date && x.StartTime == startTime && x.TeacherId.HasValue)
                    .Select(x => x.TeacherId!.Value)
                    .Distinct()
                    .ToListAsync();

            candidates = candidates
                .Where(x => !usedUserIdsAtSameTime.Contains(x.Id))
                .ToList();

            var history = normalizedRoleName == "driver"
                ? await _busRunRepo.Get()
                    .Where(x => x.DriverId.HasValue)
                    .Select(x => new StaffHistorySourceRow
                    {
                        UserId = x.DriverId!.Value,
                        ServiceDate = x.ServiceDate,
                        StartTime = x.StartTime
                    })
                    .ToListAsync()
                : await _busRunRepo.Get()
                    .Where(x => x.TeacherId.HasValue)
                    .Select(x => new StaffHistorySourceRow
                    {
                        UserId = x.TeacherId!.Value,
                        ServiceDate = x.ServiceDate,
                        StartTime = x.StartTime
                    })
                    .ToListAsync();

            var ranking = history
                .Select(x => new StaffHistoryRow
                {
                    UserId = x.UserId,
                    ScheduledAt = x.ServiceDate.Date.Add(x.StartTime)
                })
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    x => x.Key,
                    x => new
                    {
                        Count = x.Count(),
                        LastAssignedAt = x.Max(y => y.ScheduledAt)
                    });

            var selectedIds = candidates
                .OrderBy(x => ranking.TryGetValue(x.Id, out var info) ? info.Count : 0)
                .ThenBy(x => ranking.TryGetValue(x.Id, out var info) ? info.LastAssignedAt : DateTime.MinValue)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(requiredCount)
                .Select(x => (long?)x.Id)
                .ToList();

            if (selectedIds.Count < requiredCount)
                throw new Exception($"Không đủ {normalizedRoleName} rảnh để tự động gán cho tất cả bus run. Cần {requiredCount}, hiện chỉ còn {selectedIds.Count}");

            return selectedIds;
        }

        private List<int> BuildPrimaryBusLoads(int totalStudents)
        {
            if (totalStudents <= 0)
                throw new Exception("Tổng số học sinh phải lớn hơn 0");

            // Rule nghiep vu:
            // - So xe chinh duoc tinh tu tong hoc sinh / 20
            // - Neu phan thap phan <= 0.5 => lay phan nguyen (bo thap phan)
            // - Neu phan thap phan > 0.5 => cong them 1 xe
            // Vi du:
            // 50/20 = 2.5 => 2 xe chinh (chia 25-25)
            // 52/20 = 2.6 => 3 xe chinh (chia deu)
            var rawBusCount = totalStudents / 20d;
            var floorBusCount = (int)Math.Floor(rawBusCount);
            var fractional = rawBusCount - floorBusCount;

            var busCount = fractional > 0.5d ? floorBusCount + 1 : floorBusCount;
            if (busCount <= 0)
                busCount = 1;

            return BuildBalancedLoads(totalStudents, busCount);
        }

        private static List<int> BuildBalancedLoads(int totalStudents, int busCount)
        {
            var baseLoad = totalStudents / busCount;
            var remainder = totalStudents % busCount;
            var loads = new List<int>();

            for (var i = 0; i < busCount; i++)
                loads.Add(baseLoad + (i < remainder ? 1 : 0));

            return loads.OrderByDescending(x => x).ToList();
        }

        private static List<List<Booking>> BuildBalancedAssignmentsByPickup(
            List<BusRouteStation> routeStations,
            List<Booking> bookings,
            List<int> loads)
        {
            var assignments = loads.Select(_ => new List<Booking>()).ToList();
            var remainingLoads = loads.ToArray();
            var routeStationOrder = routeStations.ToDictionary(x => x.StationId, x => x.OrderIndex);

            var stationGroups = bookings
                .OrderBy(x => routeStationOrder.TryGetValue(x.StationId, out var orderIndex) ? orderIndex : int.MaxValue)
                .ThenBy(x => x.Latitude ?? double.MaxValue)
                .ThenBy(x => x.Longitude ?? double.MaxValue)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .GroupBy(x => x.StationId)
                .ToList();

            foreach (var stationGroup in stationGroups)
            {
                var stationBookings = stationGroup.ToList();
                var pointer = 0;

                while (pointer < stationBookings.Count)
                {
                    var candidateRunIndexes = Enumerable.Range(0, remainingLoads.Length)
                        .Where(index => remainingLoads[index] > 0)
                        .OrderByDescending(index => remainingLoads[index])
                        .ThenBy(index => index)
                        .ToList();

                    if (!candidateRunIndexes.Any())
                        throw new Exception("Không thể phân bổ học sinh vào các xe đã tính");

                    foreach (var runIndex in candidateRunIndexes)
                    {
                        if (pointer >= stationBookings.Count)
                            break;

                        assignments[runIndex].Add(stationBookings[pointer]);
                        remainingLoads[runIndex]--;
                        pointer++;
                    }
                }
            }

            if (remainingLoads.Any(x => x != 0))
                throw new Exception("Không thể cân bằng học sinh vào đúng số lượng mỗi xe");

            return assignments;
        }

        private (List<(Bus bus, int usableCapacity)> PrimaryBuses, Bus BackupBus) SelectBusesForAssignment(
            List<int> loads,
            List<Bus> availableBuses)
        {
            var buses25 = new Queue<Bus>(availableBuses.Where(x => x.Capacity >= 25));
            var buses15 = new Queue<Bus>(availableBuses.Where(x => x.Capacity >= 15 && x.Capacity < 25));
            var selections = new List<(Bus bus, int usableCapacity)>();

            foreach (var load in loads)
            {
                if (load > 25)
                    throw new Exception("So hoc sinh tren mot xe vuot qua muc su dung 25 hoc sinh cua xe 25 cho");

                if (buses25.Count == 0)
                    throw new Exception("Không đủ xe 25 chỗ để chia học sinh vào xe chính");

                selections.Add((buses25.Dequeue(), 25));
            }

            if (buses15.Count == 0)
                throw new Exception("Không có xe 15 chỗ để chạy backup cho khung giờ này");

            return (selections, buses15.Dequeue());
        }

        private static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new Exception("Status không được để trống");

            var normalizedStatus = status.Trim().ToUpperInvariant();
            if (!AllowedStatuses.Contains(normalizedStatus))
                throw new Exception("Status chi chap nhan PENDING, CONFIRMED hoac CANCELLED");

            return normalizedStatus;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ResolveGuardianTodayStatus(
            bool hasCheckedInOnThisBus,
            bool isCurrentlyOnThisBus,
            bool isOnDifferentBusThanAssigned)
        {
            if (isCurrentlyOnThisBus)
                return "ON_ASSIGNED_BUS";

            if (isOnDifferentBusThanAssigned)
                return "ON_DIFFERENT_BUS";

            if (hasCheckedInOnThisBus)
                return "CHECKED_OUT";

            return "NOT_CHECKED_IN";
        }

        private static BusStation ResolveStation(
            List<BusRouteStation> routeStations,
            long? stationId,
            double? latitude,
            double? longitude,
            string stationType)
        {
            if (stationId.HasValue && stationId.Value > 0)
            {
                return routeStations
                    .Where(x => x.StationId == stationId.Value)
                    .Select(x => x.Station)
                    .FirstOrDefault()
                    ?? throw new Exception($"Điểm {stationType} không thuộc route đã chọn");
            }

            if (!latitude.HasValue || !longitude.HasValue)
                throw new Exception($"Can cung cap {stationType}StationId hoac lat/lng cua diem {stationType}");

            var nearestStation = routeStations
                .Where(x => x.Station.Latitude.HasValue && x.Station.Longitude.HasValue)
                .Select(x => new
                {
                    Station = x.Station,
                    Distance = CalculateDistanceKm(
                        latitude.Value,
                        longitude.Value,
                        x.Station.Latitude!.Value,
                        x.Station.Longitude!.Value)
                })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            return nearestStation?.Station
                ?? throw new Exception($"Không tìm thấy trạm có tọa độ để xác định điểm {stationType} gần nhất");
        }

        private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371d;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLng = DegreesToRadians(lng2 - lng1);
            var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                    Math.Cos(DegreesToRadians(lat1)) *
                    Math.Cos(DegreesToRadians(lat2)) *
                    Math.Pow(Math.Sin(dLng / 2), 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180d;
        }

        private static BusRunDto MapRunToDto(
            BusRun run,
            List<BusRunStudent> allStudents,
            List<Attendance> attendanceRows)
        {
            var assignedStudents = allStudents
                .Where(x => x.BusRunId == run.Id)
                .GroupBy(x => x.StudentId)
                .Select(x => x.OrderBy(y => y.Id).First())
                .ToList();

            var attendanceStudentIdsOnThisBus = attendanceRows
                .Where(a =>
                    a.BusId == run.BusId &&
                    a.CheckInTime.HasValue &&
                    !a.CheckOutTime.HasValue)
                .Select(a => a.StudentId)
                .Distinct()
                .ToList();

            var additionalStudentsOnThisBus = allStudents
                .Where(x =>
                    attendanceStudentIdsOnThisBus.Contains(x.StudentId) &&
                    x.BusRunId != run.Id)
                .GroupBy(x => x.StudentId)
                .Select(x => x.OrderBy(y => y.Id).First())
                .Where(x => assignedStudents.All(y => y.StudentId != x.StudentId))
                .ToList();

            var displayStudents = assignedStudents
                .Concat(additionalStudentsOnThisBus)
                .OrderBy(x => x.Student.StudentCode)
                .ThenBy(x => x.Student.FullName)
                .ToList();

            return new BusRunDto
            {
                Id = run.Id,
                RouteId = run.RouteId,
                RouteName = run.Route.Name,
                ServiceDate = run.ServiceDate,
                StartTime = run.StartTime,
                BusId = run.BusId,
                BusLabel = !string.IsNullOrWhiteSpace(run.Bus.BusNumber) ? run.Bus.BusNumber : run.Bus.LicensePlate,
                DriverId = run.DriverId,
                DriverName = run.Driver?.FullName ?? run.Driver?.Email,
                TeacherId = run.TeacherId,
                TeacherName = run.Teacher?.FullName ?? run.Teacher?.Email,
                SeatCapacity = run.SeatCapacity,
                UsableCapacity = run.UsableCapacity,
                AssignedStudentCount = run.AssignedStudentCount,
                RunOrder = run.RunOrder,
                Status = run.Status,
                Students = displayStudents
                    .Select(x =>
                    {
                        var checkInOnThisBus = attendanceRows.Any(a =>
                            a.StudentId == x.StudentId &&
                            a.BusId == run.BusId &&
                            a.CheckInTime.HasValue);

                        var currentlyOnThisBus = attendanceRows.Any(a =>
                            a.StudentId == x.StudentId &&
                            a.BusId == run.BusId &&
                            a.CheckInTime.HasValue &&
                            !a.CheckOutTime.HasValue);

                        var activeOnOtherBus = attendanceRows.FirstOrDefault(a =>
                            a.StudentId == x.StudentId &&
                            a.BusId != run.BusId &&
                            a.CheckInTime.HasValue &&
                            !a.CheckOutTime.HasValue);

                        var currentBusLabel = activeOnOtherBus?.Bus != null
                            ? (!string.IsNullOrWhiteSpace(activeOnOtherBus.Bus.BusNumber)
                                ? activeOnOtherBus.Bus.BusNumber
                                : activeOnOtherBus.Bus.LicensePlate)
                            : null;

                        return new BusRunStudentDto
                        {
                            BookingId = x.BookingId,
                            StudentId = x.StudentId,
                            StudentCode = x.Student.StudentCode,
                            StudentName = x.Student.FullName,
                            StationId = x.Booking.StationId,
                            StationName = x.Booking.Station.Name,
                            PickupAddress = x.Booking.PickupAddress,
                            HasCheckedInOnThisBus = checkInOnThisBus,
                            IsCurrentlyOnThisBus = currentlyOnThisBus,
                            CurrentBusId = activeOnOtherBus?.BusId,
                            CurrentBusLabel = currentBusLabel,
                            IsOnDifferentBusThanAssigned = activeOnOtherBus != null
                        };
                    })
                    .ToList()
            };
        }

        private static BookingDto MapToDto(Booking booking)
        {
            return new BookingDto
            {
                Id = booking.Id,
                StudentId = booking.StudentId,
                StudentCode = booking.Student.StudentCode,
                StudentName = booking.Student.FullName,
                GuardianId = booking.Student.GuardianId,
                GuardianName = booking.Student.Guardian?.FullName ?? string.Empty,
                RouteId = booking.RouteId,
                RouteName = booking.Route.Name,
                ServiceDate = booking.ServiceDate,
                StartTime = booking.StartTime,
                StationId = booking.StationId,
                StationName = booking.Station.Name,
                StationAddress = booking.Station.Address,
                PickupAddress = booking.PickupAddress,
                Latitude = booking.Latitude,
                Longitude = booking.Longitude,
                Status = booking.Status,
                Note = booking.Note,
                CreatedAt = booking.CreatedAt
            };
        }

        private async Task<BusRunDto> GetBusRunByIdAsync(long busRunId)
        {
            var busRun = await _busRunRepo.Get()
                .Include(x => x.Route)
                .Include(x => x.Bus)
                .Include(x => x.Driver)
                .ThenInclude(x => x.Role)
                .Include(x => x.Teacher)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == busRunId)
                ?? throw new Exception("Bus run không tồn tại");

            var students = await _busRunStudentRepo.Get()
                .Include(x => x.Student)
                .Include(x => x.Booking)
                .ThenInclude(x => x.Station)
                .Where(x => x.BusRun.RouteId == busRun.RouteId && x.BusRun.ServiceDate.Date == busRun.ServiceDate.Date && x.BusRun.StartTime == busRun.StartTime)
                .ToListAsync();
            var studentIds = students.Select(x => x.StudentId).Distinct().ToList();
            var attendanceRows = await _attendanceRepo.Get()
                .Include(x => x.Bus)
                .Where(x =>
                    studentIds.Contains(x.StudentId) &&
                    x.Date.Date == busRun.ServiceDate.Date)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return MapRunToDto(busRun, students, attendanceRows);
        }

        private async Task<User> ValidateUserByRoleAsync(long userId, string roleName)
        {
            if (userId <= 0)
                throw new Exception($"{Capitalize(roleName)}Id phải lớn hơn 0");

            var user = await _userRepo.Get()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception($"{Capitalize(roleName)} không tồn tại");

            if (!string.Equals(user.Role.Name, roleName, StringComparison.OrdinalIgnoreCase))
                throw new Exception($"User được chọn không phải {roleName}");

            if (user.Status != AccountStatus.ACTIVE)
                throw new Exception($"{Capitalize(roleName)} đang không hoạt động");

            if (string.Equals(roleName, "driver", StringComparison.OrdinalIgnoreCase) &&
                user.DriverLicenseExpiryDate.HasValue &&
                user.DriverLicenseExpiryDate.Value.Date <= _appTime.TodayDate)
            {
                throw new Exception("Driver da het han bang lai");
            }

            return user;
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

        private async Task EnsureBusRunStaffAvailabilityAsync(BusRun busRun, long? driverId, long? teacherId)
        {
            if (driverId.HasValue)
            {
                var sameDriver = await _busRunRepo.Get()
                    .AnyAsync(x =>
                        x.Id != busRun.Id &&
                        x.ServiceDate.Date == busRun.ServiceDate.Date &&
                        x.StartTime == busRun.StartTime &&
                        x.DriverId == driverId.Value);

                if (sameDriver)
                    throw new Exception("Driver đã được gán cho chuyến khác cùng khung giờ");
            }

            if (teacherId.HasValue)
            {
                var sameTeacher = await _busRunRepo.Get()
                    .AnyAsync(x =>
                        x.Id != busRun.Id &&
                        x.ServiceDate.Date == busRun.ServiceDate.Date &&
                        x.StartTime == busRun.StartTime &&
                        x.TeacherId == teacherId.Value);

                if (sameTeacher)
                    throw new Exception("Teacher đã được gán cho chuyến khác cùng khung giờ");
            }
        }

        private static string Capitalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? value
                : char.ToUpperInvariant(value[0]) + value[1..];
        }

        private sealed class StaffAssignmentPlan
        {
            public List<long?> DriverIds { get; set; } = new();
            public List<long?> TeacherIds { get; set; } = new();
        }

        private sealed class StaffHistoryRow
        {
            public long UserId { get; set; }
            public DateTime ScheduledAt { get; set; }
        }

        private sealed class StaffHistorySourceRow
        {
            public long UserId { get; set; }
            public DateTime ServiceDate { get; set; }
            public TimeSpan StartTime { get; set; }
        }
    }
}
