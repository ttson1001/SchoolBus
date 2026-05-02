using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        private const string NOTIFICATION_LIST_SUCCESS = "Lấy danh sách thông báo thành công";

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Pha huynh / user: ba query userId a lay thAng bAo caa chAnh mAnh (theo JWT).
        /// Admin: cA tha truyan userId Aa xem thAng bAo user AA. User thang cha Aac userId trAng vai token.
        /// </summary>
        [HttpGet("[action]")]
        public async Task<IActionResult> Search(
            long? userId,
            bool? isRead,
            string? type,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 20)
        {
            var response = new ResponseDto();

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRole = GetCurrentRole();

                var data = await _notificationService.SearchAsync(
                    currentUserId,
                    currentRole,
                    userId,
                    isRead,
                    type,
                    fromDate,
                    toDate,
                    page,
                    pageSize);

                response.Data = data;
                response.Message = NOTIFICATION_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        private long GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue("UserId");

            if (string.IsNullOrWhiteSpace(userIdValue) || !long.TryParse(userIdValue, out var id))
                throw new Exception("KhAng Aac Aac UserId ta token");

            return id;
        }

        private string GetCurrentRole()
        {
            var role = User.FindFirstValue("Role");
            return string.IsNullOrWhiteSpace(role) ? string.Empty : role;
        }
    }
}
