namespace BE_API.Configuration
{
    public class CompreFaceSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:8000";
        public string ApiKey { get; set; } = string.Empty;
        public decimal SimilarityThreshold { get; set; } = 0.90m;
    }
}
