namespace BE_API.Dto.FaceRecognition
{
    public class FaceSubjectImagesDto
    {
        public string Subject { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public List<FaceSubjectImageDto> Items { get; set; } = new();
    }
}
