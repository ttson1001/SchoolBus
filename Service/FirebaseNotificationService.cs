using BE_API.Configuration;
using BE_API.Service.IService;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;
using FirebaseMessageNotification = FirebaseAdmin.Messaging.Notification;

namespace BE_API.Service
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly FirebaseSettings _firebaseSettings;
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(
            IOptions<FirebaseSettings> firebaseOptions,
            ILogger<FirebaseNotificationService> logger)
        {
            _firebaseSettings = firebaseOptions.Value;
            _logger = logger;
        }

        public async Task<bool> SendAsync(
            string? deviceToken,
            string title,
            string body,
            IDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (!_firebaseSettings.Enabled)
                return false;

            if (string.IsNullOrWhiteSpace(deviceToken))
                return false;

            var app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                _logger.LogWarning("Firebase chưa được khởi tạo nên không thể gửi push notification.");
                return false;
            }

            var message = new Message
            {
                Token = deviceToken.Trim(),
                Notification = new FirebaseMessageNotification
                {
                    Title = string.IsNullOrWhiteSpace(title) ? "Thông báo SchoolBus" : title.Trim(),
                    Body = body?.Trim() ?? string.Empty
                },
                Data = data == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(data
                        .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value != null)
                        .ToDictionary(x => x.Key, x => x.Value))
            };

            try
            {
                var messaging = FirebaseMessaging.GetMessaging(app);
                await messaging.SendAsync(message, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi Firebase notification thất bại cho device token.");
                return false;
            }
        }
    }
}
