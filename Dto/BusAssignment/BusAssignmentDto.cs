using BE_API.Dto.Bus;
using BE_API.Dto.BusRoute;
using BE_API.Dto.BusSchedule;
using BE_API.Dto.User;

namespace BE_API.Dto.BusAssignment
{
    public class BusAssignmentDto
    {
        public long Id { get; set; }
        public long BusScheduleId { get; set; }
        public BusScheduleDto BusSchedule { get; set; } = null!;
        public long DriverId { get; set; }
        public UserDto Driver { get; set; } = null!;
        public long TeacherId { get; set; }
        public UserDto Teacher { get; set; } = null!;
        public DateTime? ActiveDate { get; set; }
    }
}
