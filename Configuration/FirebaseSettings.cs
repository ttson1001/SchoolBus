namespace BE_API.Configuration
{
    public class FirebaseSettings
    {
        public bool Enabled { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public string CredentialsPath { get; set; } = string.Empty;

        /// <summary>
        /// Khi true, cho phép POST /api/FirebaseTest/Send không cần JWT ngay cả khi không phải Development (cẩn thận spam FCM).
        /// </summary>
        public bool AllowPublicTestEndpoint { get; set; }
    }
}
