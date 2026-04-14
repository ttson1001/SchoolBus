namespace BE_API.Dto.FaceRecognition
{
    public class SimilarityThresholdDto
    {
        public decimal SimilarityThreshold { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
