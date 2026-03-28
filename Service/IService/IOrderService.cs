using BE_API.Dto.Order;
using PayOS.Models.Webhooks;

namespace BE_API.Service.IService
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(OrderCreateDto dto);
        Task<OrderPayOsLinkDto> CreatePayOsOrderLinkAsync(OrderPayOsCreateDto dto);
        Task<OrderPayOsStatusDto> HandlePayOsWebhookAsync(Webhook webhook);
        Task<OrderPayOsStatusDto> GetPayOsOrderStatusAsync(long orderCode);
        Task<OrderDto> GetOrderByIdAsync(long id);
        Task<List<OrderDto>> GetOrdersByGuardianIdAsync(long guardianId);
        Task<OrderDto?> GetActiveOrderByStudentIdAsync(long studentId);
    }
}
