namespace BE_API.Common
{
    public static class ScheduleDayOfWeek
    {
        // Quy ước của hệ thống: Thứ 2 = 0, ..., Chủ nhật = 6.
        public static int FromDate(DateTime date)
        {
            return ((int)date.DayOfWeek + 6) % 7;
        }
    }
}
