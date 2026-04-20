namespace BE_API.Common
{
    /// <summary>
    /// Đồng hồ ứng dụng: một múi giờ cho toàn bộ "ngày lịch" và DayOfWeek nghiệp vụ.
    /// Timestamps lưu DB (UTC) vẫn dùng <see cref="UtcNow"/>.
    /// </summary>
    public interface IAppTime
    {
        TimeZoneInfo TimeZone { get; }

        DateTime UtcNow { get; }

        /// <summary>Ngày lịch hiện tại tại múi ứng dụng (00:00:00, Kind Unspecified).</summary>
        DateTime TodayDate { get; }

        DateOnly Today { get; }

        /// <summary>Giờ trong ngày tại múi ứng dụng (điểm danh, lịch tài xế).</summary>
        TimeSpan GetTimeOfDay();

        /// <summary>Ngày lịch tại múi ứng dụng của một mốc UTC (RideDate, validate lịch).</summary>
        DateTime GetCalendarDateForUtc(DateTime utc);

        /// <summary>
        /// Tham số ngày từ API: UTC → ngày lịch app; Local → ngày lịch local; Unspecified → .Date (FE gửi yyyy-MM-dd).
        /// </summary>
        DateTime GetRideCalendarDate(DateTime? rideDate);

        /// <summary>Chuẩn hóa instant từ client về UTC (ArrivedAt).</summary>
        DateTime NormalizeToUtc(DateTime value);
    }
}
