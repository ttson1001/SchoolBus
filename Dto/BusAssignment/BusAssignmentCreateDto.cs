namespace BE_API.Dto.BusAssignment
{
    public class BusAssignmentCreateDto
    {
        public long BusId { get; set; }
        public long DriverId { get; set; }
        public long TeacherId { get; set; }
        public DateTime? ActiveDate { get; set; }
    }
}
