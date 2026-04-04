using BE_API.Dto.Common;
using BE_API.Dto.TransactionLog;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionLogController : ControllerBase
    {
        private readonly ITransactionLogService _transactionLogService;

        private const string TRANSACTION_LOG_GET_SUCCESS = "Lấy transaction log thành công";
        private const string TRANSACTION_LOG_LIST_SUCCESS = "Lấy danh sách transaction log thành công";
        private const string TRANSACTION_LOG_CREATE_SUCCESS = "Tạo transaction log thành công";
        private const string TRANSACTION_LOG_UPDATE_SUCCESS = "Cập nhật transaction log thành công";
        private const string TRANSACTION_LOG_DELETE_SUCCESS = "Xóa transaction log thành công";

        public TransactionLogController(ITransactionLogService transactionLogService)
        {
            _transactionLogService = transactionLogService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search(string? keyword, string? method, string? status, long? orderId, string? code, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _transactionLogService.SearchAsync(keyword, method, status, orderId, code, fromDate, toDate, page, pageSize);
                response.Data = data;
                response.Message = TRANSACTION_LOG_LIST_SUCCESS;
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
                var data = await _transactionLogService.GetByIdAsync(id);
                response.Data = data;
                response.Message = TRANSACTION_LOG_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] TransactionLogCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _transactionLogService.CreateAsync(dto);
                response.Data = data;
                response.Message = TRANSACTION_LOG_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] TransactionLogUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _transactionLogService.UpdateAsync(id, dto);
                response.Data = data;
                response.Message = TRANSACTION_LOG_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _transactionLogService.DeleteAsync(id);
                response.Message = TRANSACTION_LOG_DELETE_SUCCESS;
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
