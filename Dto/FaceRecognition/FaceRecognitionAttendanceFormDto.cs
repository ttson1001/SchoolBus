using Microsoft.AspNetCore.Http;

namespace BE_API.Dto.FaceRecognition
{
    public class FaceRecognitionAttendanceFormDto
    {
        public IFormFile File { get; set; } = null!;
        public long BusId { get; set; }
        public long StationId { get; set; }
        public DateTime? Date { get; set; }
        public TimeSpan? Time { get; set; }
    }
}
