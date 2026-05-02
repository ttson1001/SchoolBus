namespace BE_API.Configuration
{
    public class BookingSlotSettings
    {
        public const string SectionName = "BookingSlots";

        public int StartHour { get; set; } = 6;
        public int EndHour { get; set; } = 18;
        public int StepMinutes { get; set; } = 60;

        /// <summary>Gia cang (HH:mm hoac H:mm), vA da ca sAng / tra / chiau.</summary>
        public List<string> HardSlotTimes { get; set; } =
        [
            "06:00", "07:00", "08:00", "11:00", "12:00", "16:00", "17:00", "18:00"
        ];

        public int HardSlotAdvanceDays { get; set; } = 1;
        public int SoftSlotAdvanceDays { get; set; } = 2;

        /// <summary>Sa hac sinh tai thiau Aa maY chuyan vai khung gia mam; thap hn thA hay booking vA bAo pha huynh.</summary>
        public int SoftSlotMinStudents { get; set; } = 20;
    }
}
