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
            var (sent, _) = await SendDiagnosticAsync(deviceToken, title, body, data, cancellationToken);
            return sent;
        }

        public async Task<(bool Sent, string Detail)> SendDiagnosticAsync(
            string? deviceToken,
            string title,
            string body,
            IDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (!_firebaseSettings.Enabled)
                return (false, "Firebase:Enabled = false trong cau hAnh (appsettings hoac Firebase__Enabled).");

            if (string.IsNullOrWhiteSpace(deviceToken))
                return (false, "Thiau FCM registration token (device token rang).");

            var app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                _logger.LogWarning("Firebase cha Aac khaYi tao nAn khAng tha gai push notification.");
                return (false, "Firebase Admin SDK cha khaYi tao. Kiam tra Firebase:CredentialsPath, file JSON tan tai, vA log lAc start API.");
            }

            var message = new Message
            {
                Token = deviceToken.Trim(),
                Notification = new FirebaseMessageNotification
                {
                    Title = string.IsNullOrWhiteSpace(title) ? "ThAng bAo SchoolBus" : title.Trim(),
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
                return (true, "AA gai tai FCM thAnh cAng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gai Firebase notification that bai cho device token.");
                return (false, $"Lai ta FCM: {ex.Message}");
            }
        }
    }
}
