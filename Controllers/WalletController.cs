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

        private const string WALLET_GET_SUCCESS = "Lay vi thanh cong";
        private const string WALLET_TOPUP_SUCCESS = "Nap tien vao vi thanh cong";
        private const string WALLET_PAYOS_LINK_SUCCESS = "Tao link nap tien payOS thanh cong";
        private const string WALLET_PAYOS_WEBHOOK_SUCCESS = "Xu ly webhook payOS thanh cong";

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("[action]/{userId}")]
        [SwaggerOperation(Summary = "Lay vi theo user id")]
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
        [SwaggerOperation(Summary = "Nap tien vao vi guardian")]
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
        [SwaggerOperation(Summary = "Tao link nap tien bang payOS")]
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
        [SwaggerOperation(Summary = "Nhan webhook payOS de cong tien vao vi")]
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
        [SwaggerOperation(Summary = "Lay trang thai giao dich nap tien payOS")]
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
