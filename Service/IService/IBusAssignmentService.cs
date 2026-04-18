using BE_API.Dto.BusAssignment;
using BE_API.Dto.Common;

namespace BE_API.Service.IService
{
    public interface IBusAssignmentService
    {
        Task<PagedResult<BusAssignmentDto>> SearchAsync(string? keyword, long? busId, long? driverId, long? teacherId, int page, int pageSize);
        Task<BusAssignmentDto> GetByIdAsync(long id);
        Task<BusAssignmentDto> CreateAsync(BusAssignmentCreateDto dto);
        Task<BusAssignmentDto> UpdateAsync(long id, BusAssignmentUpdateDto dto);
        Task DeleteAsync(long id);
    }
}
