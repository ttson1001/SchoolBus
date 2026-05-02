namespace BE_API.Configuration
{
    public class AppTimeSettings
    {
        public const string SectionName = "AppTime";

        /// <summary>IANA (Linux/macOS) hoac Windows ID, vA da Asia/Ho_Chi_Minh hoac SE Asia Standard Time.</summary>
        public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    }
}
