namespace BE_API.Entites
{
    public class FaceRecognitionLog : IEntity
    {
        public long Id { get; set; }

        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public string? ImageUrl { get; set; }
        public DateTime RecognizedAt { get; set; } = DateTime.UtcNow;
        public decimal ConfidenceScore { get; set; }
    }
}
