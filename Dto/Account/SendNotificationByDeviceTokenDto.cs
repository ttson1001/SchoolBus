namespace BE_API.Dto.Account
{
    public class SendNotificationByDeviceTokenDto
    {
        public string? DeviceToken { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
    }
}
