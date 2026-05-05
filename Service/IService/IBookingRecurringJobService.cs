namespace BE_API.Service.IService
{
    public interface IBookingRecurringJobService
    {
        Task AutoAssignTomorrowBusRunsAsync();
        Task FinalizeTomorrowSoftBookingsAsync();
        Task ProcessTodayBookingReminderNotificationsAsync();
    }
}
