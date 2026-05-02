namespace BE_API.Configuration
{
    public class FirebaseSettings
    {
        public bool Enabled { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public string CredentialsPath { get; set; } = string.Empty;

        /// <summary>
        /// Khi true, cho phAp POST /api/FirebaseTest/Send khAng can JWT ngay ca khi khAng phai Development (can than spam FCM).
        /// </summary>
        public bool AllowPublicTestEndpoint { get; set; }
    }
}
