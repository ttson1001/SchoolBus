namespace BE_API.Dto.Firebase
{
    public class FirebaseStatusDto
    {
        public bool ConfigEnabled { get; set; }
        public bool AdminSdkInitialized { get; set; }
        public string? ProjectId { get; set; }
        public bool CredentialsFileExists { get; set; }
        public string? CredentialsPathResolved { get; set; }
    }

    public class FirebaseSendTestDto
    {
        /// <summary>
        /// Optional. If omitted, uses DeviceToken from the current user's profile (after login).
        /// </summary>
        public string? DeviceToken { get; set; }
    }

    /// <summary>
    /// Body cho API test FCM công khai (không JWT).
    /// </summary>
    public class FirebasePublicTestDto
    {
        /// <summary>
        /// FCM registration token từ thiết bị.
        /// </summary>
        public string DeviceIdToken { get; set; } = null!;
    }
}
