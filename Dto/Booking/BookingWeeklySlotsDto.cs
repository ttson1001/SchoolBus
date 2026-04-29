namespace BE_API.Dto.Booking
{
    /// <summary>
    /// Cửa sổ từ hôm nay đến hết ngày thứ 7 sau (8 ngày), cùng khung slot theo cấu hình mỗi ngày.
    /// </summary>
    public class BookingWeeklySlotsDto
    {
        /// <summary>Ngày đầu (hôm nay theo AppTime).</summary>
        public string WeekStartDate { get; set; } = string.Empty;

        /// <summary>Ngày cuối (hôm nay + 7).</summary>
        public string WeekEndDate { get; set; } = string.Empty;

        /// <summary>Khung giờ (áp dụng mọi ngày trong danh sách) để sinh soft / lọc hard.</summary>
        public int StartHour { get; set; }

        public int EndHour { get; set; }

        public IReadOnlyList<BookingDayTimeSlotsDto> Days { get; set; } = Array.Empty<BookingDayTimeSlotsDto>();
    }

    public class BookingDayTimeSlotsDto
    {
        public string Date { get; set; } = string.Empty;

        /// <summary>0 = Chủ nhật … 6 = Thứ bảy (theo <see cref="DayOfWeek"/>).</summary>
        public int DayOfWeek { get; set; }

        public string DayName { get; set; } = string.Empty;

        /// <summary>Mỗi mốc trên lưới (StepMinutes): <c>kind</c> = <c>hard</c> nếu nằm trong HardSlotTimes, ngược lại <c>soft</c>.</summary>
        public IReadOnlyList<BookingWeekSlotItemDto> Slots { get; set; } = Array.Empty<BookingWeekSlotItemDto>();
    }

    public class BookingWeekSlotItemDto
    {
        /// <summary>Giờ bắt đầu slot, HH:mm.</summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary><c>hard</c> hoặc <c>soft</c>.</summary>
        public string Kind { get; set; } = string.Empty;
    }
}
