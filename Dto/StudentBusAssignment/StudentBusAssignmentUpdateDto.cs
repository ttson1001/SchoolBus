namespace BE_API.Dto.StudentBusAssignment
{
    public class StudentBusAssignmentUpdateDto
    {
        public long? StudentId { get; set; }
        public long? RouteId { get; set; }
        public DateTime? RideDate { get; set; }
        public long? PickupStationId { get; set; }
        public long? DropOffStationId { get; set; }
    }
}
