using BE_API.Dto.Order;

namespace BE_API.Service.IService
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(OrderCreateDto dto);
        Task<OrderDto> GetOrderByIdAsync(long id);
        Task<List<OrderDto>> GetOrdersByGuardianIdAsync(long guardianId);
        Task<OrderDto?> GetActiveOrderByStudentIdAsync(long studentId);
    }
}
