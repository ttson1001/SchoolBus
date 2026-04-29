namespace BE_API.Configuration
{
    public class BookingSlotSettings
    {
        public const string SectionName = "BookingSlots";

        public int StartHour { get; set; } = 6;
        public int EndHour { get; set; } = 18;
        public int StepMinutes { get; set; } = 60;

        /// <summary>Giờ cứng (HH:mm hoặc H:mm), ví dụ ca sáng / trưa / chiều.</summary>
        public List<string> HardSlotTimes { get; set; } =
        [
            "06:00", "07:00", "08:00", "11:00", "12:00", "16:00", "17:00", "18:00"
        ];

        /// <summary>Số học sinh tối thiểu để mở chuyến với khung giờ mềm; thấp hơn thì hủy booking và báo phụ huynh.</summary>
        public int SoftSlotMinStudents { get; set; } = 20;
    }
}

