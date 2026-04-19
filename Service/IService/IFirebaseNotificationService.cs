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
        /// Giống SendAsync nhưng trả thêm mô tả lý do khi không gửi được (dùng cho API test / debug).
        /// </summary>
        Task<(bool Sent, string Detail)> SendDiagnosticAsync(
            string? deviceToken,
            string title,
            string body,
            IDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);
    }
}
