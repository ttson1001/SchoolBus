namespace BE_API.Configuration
{
    public class FirebaseSettings
    {
        public bool Enabled { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public string CredentialsPath { get; set; } = string.Empty;
    }
}
