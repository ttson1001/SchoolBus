using BE_API.Dto.BusRoute;
using BE_API.Dto.Common;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusRouteController : ControllerBase
    {
        private readonly IBusRouteService _busRouteService;

        private const string BUS_ROUTE_CREATE_SUCCESS = "Tạo bus route thành công";

        public BusRouteController(IBusRouteService busRouteService)
        {
            _busRouteService = busRouteService;
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo tuyến xe bus")]
        public async Task<IActionResult> Create([FromBody] BusRouteCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _busRouteService.CreateBusRouteAsync(dto);
                response.Data = data;
                response.Message = BUS_ROUTE_CREATE_SUCCESS;
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
