using BE_API.Dto.Booking;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IBookingService
    {
        Task<PagedResult<BookingDto>> SearchAsync(
            long? studentId,
            long? routeId,
            DateTime? serviceDate,
            string? status,
            int page,
            int pageSize);
        Task<BookingDto> GetByIdAsync(long id);
        Task<BookingDto> CreateAsync(BookingCreateDto dto);
        Task<BookingDto> UpdateAsync(long id, BookingUpdateDto dto);
        Task<List<BusRunDto>> GetBusRunsAsync(
            DateTime serviceDate,
            long? routeId,
            long? busId,
            long? driverId,
            long? teacherId);
        Task<List<GuardianBusRunWithTomorrowBookingDto>> GetTodayBusRunsByGuardianAsync(long guardianId, DateTime? serviceDate);
        Task<BusRunDto> AssignBusRunStaffAsync(long busRunId, BusRunAssignStaffDto dto);
        Task<List<BusRunDto>> AutoAssignBusRunsAsync(AutoAssignBookingRequestDto dto);
        Task<List<BusRunDto>> AutoAssignBusRunsByDateAsync(DateTime serviceDate);
        Task FinalizeSoftSlotBookingsForDateAsync(DateTime serviceDate);
        Task DeleteAsync(long id);

        /// <summary>HAm nay a hAm nay + 7 ngAy (8 ngAy), slot theo BookingSlots trong appsettings.</summary>
        Task<BookingWeeklySlotsDto> GetWeeklyBookingSlotsAsync();
    }
}
