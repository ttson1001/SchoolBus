using BE_API.Dto.Common;
using BE_API.Dto.Notification;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<Notification> _notificationRepo;

        public NotificationService(IRepository<Notification> notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        public async Task<PagedResult<NotificationDto>> SearchAsync(
            long currentUserId,
            string currentRole,
            long? userId,
            bool? isRead,
            string? type,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1 || pageSize > 100)
                pageSize = 10;

            var effectiveUserId = ResolveEffectiveUserId(currentUserId, currentRole, userId);

            var query = _notificationRepo.Get().Where(x => x.UserId == effectiveUserId);

            if (isRead.HasValue)
                query = query.Where(x => x.IsRead == isRead.Value);

            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim();
                query = query.Where(x => x.Type == t);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < toExclusive);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NotificationDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Message = x.Message,
                    Type = x.Type,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<NotificationDto>
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                Items = items
            };
        }

        private static long ResolveEffectiveUserId(long currentUserId, string currentRole, long? userId)
        {
            if (!userId.HasValue || userId.Value <= 0)
                return currentUserId;

            if (string.Equals(currentRole, "admin", StringComparison.OrdinalIgnoreCase))
                return userId.Value;

            if (userId.Value == currentUserId)
                return currentUserId;

            throw new Exception("Không được xem thông báo của user khác");
        }
    }
}
