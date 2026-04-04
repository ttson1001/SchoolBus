namespace BE_API.Dto.FaceRecognition
{
    public class FaceRecognitionResultDto
    {
        public bool IsMatched { get; set; }
        public long? StudentId { get; set; }
        public string? Subject { get; set; }
        public decimal ConfidenceScore { get; set; }
        public decimal SimilarityThreshold { get; set; }
        public string? Message { get; set; }
    }
}
