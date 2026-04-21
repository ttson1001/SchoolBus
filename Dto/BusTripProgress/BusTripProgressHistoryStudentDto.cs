namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressHistoryStudentDto
    {
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public long? PickupStationId { get; set; }
        public string? PickupStationName { get; set; }
        public long? DropOffStationId { get; set; }
        public string? DropOffStationName { get; set; }
        public string AssignmentType { get; set; } = null!;
    }
}
