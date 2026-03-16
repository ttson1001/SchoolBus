using BE_API.Dto.Common;
using BE_API.Dto.Order;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        private const string ORDER_GET_SUCCESS = "Lấy order thành công";
        private const string ORDER_LIST_SUCCESS = "Lấy danh sách order thành công";
        private const string ORDER_CREATE_SUCCESS = "Tạo order thành công";

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
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
    }
}
