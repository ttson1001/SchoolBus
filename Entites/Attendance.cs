using BE_API.Entites.Enums;

namespace BE_API.Entites
{
    public class Attendance : IEntity
    {
        public long Id { get; set; }

        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public long BusId { get; set; }
        public Bus Bus { get; set; } = null!;

        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }

        public AttendanceMethod Method { get; set; }
        public AttendanceStatus Status { get; set; }
    }
}
