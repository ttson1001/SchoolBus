namespace BE_API.Dto.Booking
{
    /// <summary>
    /// Caa sa ta hAm nay Aan hat ngAy tha 7 sau (8 ngAy), cAng khung slot theo cau hAnh mai ngAy.
    /// </summary>
    public class BookingWeeklySlotsDto
    {
        /// <summary>NgAy Aau (hAm nay theo AppTime).</summary>
        public string WeekStartDate { get; set; } = string.Empty;

        /// <summary>NgAy cuai (hAm nay + 7).</summary>
        public string WeekEndDate { get; set; } = string.Empty;

        /// <summary>Khung gia (Ap dang mai ngAy trong danh sAch) Aa sinh soft / lac hard.</summary>
        public int StartHour { get; set; }

        public int EndHour { get; set; }

        public IReadOnlyList<BookingDayTimeSlotsDto> Days { get; set; } = Array.Empty<BookingDayTimeSlotsDto>();
    }

    public class BookingDayTimeSlotsDto
    {
        public string Date { get; set; } = string.Empty;

        /// <summary>0 = Cha nhat a 6 = Tha bay (theo <see cref="DayOfWeek"/>).</summary>
        public int DayOfWeek { get; set; }

        public string DayName { get; set; } = string.Empty;

        /// <summary>Mai mac trAn lai (StepMinutes): <c>kind</c> = <c>hard</c> nau nam trong HardSlotTimes, ngac lai <c>soft</c>.</summary>
        public IReadOnlyList<BookingWeekSlotItemDto> Slots { get; set; } = Array.Empty<BookingWeekSlotItemDto>();
    }

    public class BookingWeekSlotItemDto
    {
        /// <summary>Gia bat Aau slot, HH:mm.</summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary><c>hard</c> hoac <c>soft</c>.</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Danh sach huong tuyen duoc phep booking trong khung gio nay.</summary>
        public IReadOnlyList<string> AllowedRouteStatuses { get; set; } = Array.Empty<string>();
    }
}
