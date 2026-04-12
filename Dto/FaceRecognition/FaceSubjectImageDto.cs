namespace BE_API.Dto.FaceRecognition
{
    public class FaceSubjectImageDto
    {
        public string ImageId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string? ImageBase64 { get; set; }
    }
}
