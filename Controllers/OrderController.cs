using BE_API.Dto.Common;
using BE_API.Dto.Order;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PayOS.Models.Webhooks;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IWalletService _walletService;
        private readonly ILogger<OrderController> _logger;

        private const string ORDER_GET_SUCCESS = "Lấy order thành công";
        private const string ORDER_LIST_SUCCESS = "Lấy danh sách order thành công";
        private const string ORDER_CREATE_SUCCESS = "Tạo order thành công";
        private const string ORDER_PAYOS_LINK_SUCCESS = "Tạo link thanh toán payOS cho order thành công";
        private const string ORDER_PAYOS_WEBHOOK_SUCCESS = "Xử lý webhook payOS thành công";
        private const string ORDER_CANCEL_SUCCESS = "Hủy order thành công";

        public OrderController(
            IOrderService orderService,
            IWalletService walletService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _walletService = walletService;
            _logger = logger;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.CreateOrderAsync(dto);
                response.Data = data;
                response.Message = ORDER_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreatePayOsLink([FromBody] OrderPayOsCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.CreatePayOsOrderLinkAsync(dto);
                response.Data = data;
                response.Message = ORDER_PAYOS_LINK_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> HandlePayOsWebhook([FromBody] Webhook webhook)
        {
            var response = new ResponseDto();

            try
            {
                var orderCode = webhook?.Data?.OrderCode;
                var code = webhook?.Data?.Code;
                var description = webhook?.Data?.Description;

                _logger.LogInformation(
                    "PayOS webhook received at OrderController. OrderCode={OrderCode}, Code={Code}, Description={Description}",
                    orderCode,
                    code,
                    description);

                object data;

                try
                {
                    data = await _orderService.HandlePayOsWebhookAsync(webhook);

                    _logger.LogInformation(
                        "PayOS webhook routed to order service successfully. OrderCode={OrderCode}",
                        orderCode);
                }
                catch (Exception ex) when (string.Equals(ex.Message, "Khong tim thay giao dich mua goi payOS", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "PayOS webhook was not matched to order transaction, falling back to wallet service. OrderCode={OrderCode}",
                        orderCode);

                    data = await _walletService.HandlePayOsWebhookAsync(webhook);

                    _logger.LogInformation(
                        "PayOS webhook routed to wallet service successfully. OrderCode={OrderCode}",
                        orderCode);
                }

                response.Data = data;
                response.Message = ORDER_PAYOS_WEBHOOK_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "PayOS webhook handling failed in OrderController. OrderCode={OrderCode}, Code={Code}",
                    webhook?.Data?.OrderCode,
                    webhook?.Data?.Code);

                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{orderCode}")]
        public async Task<IActionResult> GetPayOsStatus(long orderCode)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.GetPayOsOrderStatusAsync(orderCode);
                response.Data = data;
                response.Message = ORDER_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search(string? status, long? guardianId, long? studentId, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.SearchOrderAsync(status, guardianId, studentId, fromDate, toDate, page, pageSize);
                response.Data = data;
                response.Message = ORDER_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.GetOrderByIdAsync(id);
                response.Data = data;
                response.Message = ORDER_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{guardianId}")]
        public async Task<IActionResult> GetByGuardian(long guardianId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.GetOrdersByGuardianIdAsync(guardianId);
                response.Data = data;
                response.Message = ORDER_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{studentId}")]
        public async Task<IActionResult> GetActiveByStudent(long studentId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.GetActiveOrderByStudentIdAsync(studentId);
                response.Data = data;
                response.Message = ORDER_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> Cancel(long id, [FromBody] OrderCancelDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _orderService.CancelOrderAsync(id, dto);
                response.Data = data;
                response.Message = ORDER_CANCEL_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }
    }
}
