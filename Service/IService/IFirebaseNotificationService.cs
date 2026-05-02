namespace BE_API.Service.IService
{
    public interface IFirebaseNotificationService
    {
        Task<bool> SendAsync(
            string? deviceToken,
            string title,
            string body,
            IDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Giang SendAsync nhng tra thAm mA ta lA do khi khAng gai Aac (dAng cho API test / debug).
        /// </summary>
        Task<(bool Sent, string Detail)> SendDiagnosticAsync(
            string? deviceToken,
            string title,
            string body,
            IDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);
    }
}
