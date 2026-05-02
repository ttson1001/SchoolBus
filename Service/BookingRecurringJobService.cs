using BE_API.Common;
using BE_API.Service.IService;

namespace BE_API.Service
{
    public class BookingRecurringJobService : IBookingRecurringJobService
    {
        private readonly IBookingService _bookingService;
        private readonly IAppTime _appTime;
        private readonly ILogger<BookingRecurringJobService> _logger;

        public BookingRecurringJobService(
            IBookingService bookingService,
            IAppTime appTime,
            ILogger<BookingRecurringJobService> logger)
        {
            _bookingService = bookingService;
            _appTime = appTime;
            _logger = logger;
        }

        public async Task AutoAssignTomorrowBusRunsAsync()
        {
            var tomorrow = _appTime.TodayDate.AddDays(1);

            try
            {
                var runs = await _bookingService.AutoAssignBusRunsByDateAsync(tomorrow);
                _logger.LogInformation(
                    "Hangfire auto-assigned bus runs for {ServiceDate:yyyy-MM-dd}. Total runs: {RunCount}",
                    tomorrow,
                    runs.Count);
            }
            catch (Exception ex) when (
                string.Equals(ex.Message, "Khong co booking nao de chia xe trong ngay da chon", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("booking", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Hangfire skipped auto-assign for {ServiceDate:yyyy-MM-dd} because there are no bookings.",
                    tomorrow);
            }
        }

        public async Task FinalizeTomorrowSoftBookingsAsync()
        {
            var tomorrow = _appTime.TodayDate.AddDays(1);

            await _bookingService.FinalizeSoftSlotBookingsForDateAsync(tomorrow);

            _logger.LogInformation(
                "Hangfire finalized soft bookings for {ServiceDate:yyyy-MM-dd}.",
                tomorrow);
        }
    }
}
