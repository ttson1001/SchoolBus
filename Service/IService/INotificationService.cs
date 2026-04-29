using BE_API.Dto.Common;
using BE_API.Dto.Notification;

namespace BE_API.Service.IService
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationDto>> SearchAsync(
            long currentUserId,
            string currentRole,
            long? userId,
            bool? isRead,
            string? type,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);
    }
}
