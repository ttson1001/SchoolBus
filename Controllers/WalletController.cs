using BE_API.Dto.Common;
using BE_API.Dto.Wallet;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models;
using PayOS.Models.Webhooks;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        private const string WALLET_GET_SUCCESS = "Lấy ví thành công";
        private const string WALLET_TOPUP_SUCCESS = "Nạp tiền vào ví thành công";
        private const string WALLET_PAYOS_LINK_SUCCESS = "Tạo link nạp tiền payOS thành công";
        private const string WALLET_PAYOS_WEBHOOK_SUCCESS = "Xử lý webhook payOS thành công";

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("[action]/{userId}")]
        [SwaggerOperation(Summary = "Lấy ví theo user id")]
        public async Task<IActionResult> GetByUser(long userId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _walletService.GetWalletByUserIdAsync(userId);
                response.Data = data;
                response.Message = WALLET_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Nạp tiền vào ví guardian")]
        public async Task<IActionResult> TopUp([FromBody] WalletTopUpDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _walletService.TopUpAsync(dto);
                response.Data = data;
                response.Message = WALLET_TOPUP_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo link nạp tiền bằng payOS")]
        public async Task<IActionResult> CreatePayOsTopUpLink([FromBody] WalletPayOsCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _walletService.CreatePayOsTopUpLinkAsync(dto);
                response.Data = data;
                response.Message = WALLET_PAYOS_LINK_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Nhận webhook payOS để cộng tiền vào ví")]
        public async Task<IActionResult> HandlePayOsWebhook([FromBody] Webhook webhook)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _walletService.HandlePayOsWebhookAsync(webhook);
                response.Data = data;
                response.Message = WALLET_PAYOS_WEBHOOK_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{orderCode}")]
        [SwaggerOperation(Summary = "Lấy trạng thái giao dịch nạp tiền payOS")]
        public async Task<IActionResult> GetPayOsTopUpStatus(long orderCode)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _walletService.GetPayOsTopUpStatusAsync(orderCode);
                response.Data = data;
                response.Message = WALLET_GET_SUCCESS;
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
