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
    }
}
