using BE_API.Dto.BusSchedule;

namespace BE_API.Service.IService
{
    public interface IBusScheduleService
    {
        Task<BusScheduleDto> CreateBusScheduleAsync(BusScheduleCreateDto dto);
        Task<BusScheduleDto> GetBusScheduleByIdAsync(long id);
        Task<List<BusScheduleDto>> SearchBusSchedulesAsync(long? busId, long? routeId, long? campusId);
        Task<List<BusScheduleDto>> GetBusSchedulesByBusIdAsync(long busId);
        Task<List<BusScheduleDto>> GetBusSchedulesByRouteIdAsync(long routeId);
        Task<List<BusScheduleDto>> GetBusSchedulesByCampusIdAsync(long campusId);
        Task<List<BusScheduleDto>> GetBusSchedulesAtTimeAsync(DateTime atTime, long? campusId);
        Task<BusScheduleDto> UpdateBusScheduleAsync(long id, BusScheduleUpdateDto dto);
        Task DeleteBusScheduleAsync(long id);
    }
}
