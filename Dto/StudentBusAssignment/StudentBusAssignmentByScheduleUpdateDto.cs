namespace BE_API.Dto.StudentBusAssignment
{
    public class StudentBusAssignmentByScheduleUpdateDto
    {
        public long? StudentId { get; set; }
        public long? BusScheduleId { get; set; }
        public DateTime? RideDate { get; set; }
        public long? PickupStationId { get; set; }
        public long? DropOffStationId { get; set; }
    }
}
