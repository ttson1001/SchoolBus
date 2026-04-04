using BE_API.Dto.Order;
using BE_API.Dto.Common;
using PayOS.Models.Webhooks;

namespace BE_API.Service.IService
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(OrderCreateDto dto);
        Task<OrderPayOsLinkDto> CreatePayOsOrderLinkAsync(OrderPayOsCreateDto dto);
        Task<OrderPayOsStatusDto> HandlePayOsWebhookAsync(Webhook webhook);
        Task<OrderPayOsStatusDto> GetPayOsOrderStatusAsync(long orderCode);
        Task<PagedResult<OrderDto>> SearchOrderAsync(string? status, long? guardianId, long? studentId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
        Task<OrderDto> GetOrderByIdAsync(long id);
        Task<List<OrderDto>> GetOrdersByGuardianIdAsync(long guardianId);
        Task<OrderDto?> GetActiveOrderByStudentIdAsync(long studentId);
        Task<OrderDto> CancelOrderAsync(long id, OrderCancelDto dto);
    }
}
