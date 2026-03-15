using BE_API.Dto.BusDamageReport;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusDamageReportController : ControllerBase
    {
        private readonly IBusDamageReportService _reportService;

        private const string REPORT_LIST_SUCCESS = "Lấy danh sách báo cáo hư hỏng xe thành công";
        private const string REPORT_GET_SUCCESS = "Lấy báo cáo hư hỏng xe thành công";
        private const string REPORT_CREATE_SUCCESS = "Tạo báo cáo hư hỏng xe thành công";
        private const string REPORT_UPDATE_SUCCESS = "Cập nhật báo cáo hư hỏng xe thành công";
        private const string REPORT_DELETE_SUCCESS = "Xóa báo cáo hư hỏng xe thành công";

        public BusDamageReportController(IBusDamageReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm báo cáo hư hỏng xe")]
        public async Task<IActionResult> Search(string? keyword, string? status, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _reportService.SearchBusDamageReportAsync(keyword, status, page, pageSize);
                response.Data = data;
                response.Message = REPORT_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy báo cáo hư hỏng xe theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _reportService.GetBusDamageReportByIdAsync(id);
                response.Data = data;
                response.Message = REPORT_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Báo hư hỏng xe")]
        public async Task<IActionResult> Create([FromBody] BusDamageReportCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                await _reportService.CreateBusDamageReportAsync(dto);
                response.Message = REPORT_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật báo cáo hư hỏng xe")]
        public async Task<IActionResult> Update(long id, [FromBody] BusDamageReportUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _reportService.UpdateBusDamageReportAsync(id, dto);
                response.Data = data;
                response.Message = REPORT_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa báo cáo hư hỏng xe")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _reportService.DeleteBusDamageReportAsync(id);
                response.Message = REPORT_DELETE_SUCCESS;
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
