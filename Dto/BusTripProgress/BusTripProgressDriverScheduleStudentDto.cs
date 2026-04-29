namespace BE_API.Dto.BusTripProgress
{
    public class BusTripProgressDriverScheduleStudentDto
    {
        public long StudentId { get; set; }
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public long? StationId { get; set; }
        public string? StationName { get; set; }
        public double? PickupLatitude { get; set; }
        public double? PickupLongitude { get; set; }
    }
}
