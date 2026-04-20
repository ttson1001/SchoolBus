namespace BE_API.Configuration
{
    public class AppTimeSettings
    {
        public const string SectionName = "AppTime";

        /// <summary>IANA (Linux/macOS) hoặc Windows ID, ví dụ Asia/Ho_Chi_Minh hoặc SE Asia Standard Time.</summary>
        public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    }
}
