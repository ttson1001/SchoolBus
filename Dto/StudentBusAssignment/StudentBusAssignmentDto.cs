namespace BE_API.Dto.StudentBusAssignment
{
    public class StudentBusAssignmentDto
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public long GuardianId { get; set; }
        public long RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public DateTime? RideDate { get; set; }
        public long? PickupStationId { get; set; }
        public string? PickupStationName { get; set; }
        public long? DropOffStationId { get; set; }
        public string? DropOffStationName { get; set; }
        public string? Note { get; set; }
    }
}
