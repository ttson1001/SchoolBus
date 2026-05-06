using BE_API.Dto.Attendance;

namespace BE_API.Dto.FaceRecognition
{
    public class FaceRecognitionAttendanceResultDto
    {
        public FaceRecognitionResultDto Recognition { get; set; } = null!;
        public AttendanceDto? Attendance { get; set; }
    }
}
