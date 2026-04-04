using BE_API.Dto.Common;
using BE_API.Dto.StudentBusAssignment;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentBusAssignmentController : ControllerBase
    {
        private readonly IStudentBusAssignmentService _assignmentService;

        private const string ASSIGNMENT_GET_SUCCESS = "Lấy student bus assignment thành công";
        private const string ASSIGNMENT_LIST_SUCCESS = "Lấy danh sách student bus assignment thành công";
        private const string ASSIGNMENT_CREATE_SUCCESS = "Thiết lập điểm đón trả cho học sinh thành công";
        private const string ASSIGNMENT_UPDATE_SUCCESS = "Cập nhật student bus assignment thành công";
        private const string ASSIGNMENT_DELETE_SUCCESS = "Xóa student bus assignment thành công";

        public StudentBusAssignmentController(IStudentBusAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm student bus assignment")]
        public async Task<IActionResult> Search(
            string? keyword,
            long? studentId,
            long? guardianId,
            long? busId,
            long? routeId,
            DateTime? rideDate,
            int page = 1,
            int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.SearchAsync(keyword, studentId, guardianId, busId, routeId, rideDate, page, pageSize);
                response.Data = data;
                response.Message = ASSIGNMENT_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy student bus assignment theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.GetByIdAsync(id);
                response.Data = data;
                response.Message = ASSIGNMENT_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{studentId}")]
        [SwaggerOperation(Summary = "Lấy assignment theo student")]
        public async Task<IActionResult> GetByStudent(long studentId, DateTime? rideDate)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.GetByStudentIdAsync(studentId, rideDate);
                response.Data = data;
                response.Message = ASSIGNMENT_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{guardianId}")]
        [SwaggerOperation(Summary = "Lấy assignment theo guardian")]
        public async Task<IActionResult> GetByGuardian(long guardianId, DateTime? rideDate)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.GetByGuardianIdAsync(guardianId, rideDate);
                response.Data = data;
                response.Message = ASSIGNMENT_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Thiết lập điểm đón trả theo ngày cho học sinh")]
        public async Task<IActionResult> Create([FromBody] StudentBusAssignmentCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.CreateAsync(dto);
                response.Data = data;
                response.Message = ASSIGNMENT_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Thiết lập điểm đón trả theo bus schedule cho học sinh")]
        public async Task<IActionResult> CreateBySchedule([FromBody] StudentBusAssignmentByScheduleCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.CreateByScheduleAsync(dto);
                response.Data = data;
                response.Message = ASSIGNMENT_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật student bus assignment")]
        public async Task<IActionResult> Update(long id, [FromBody] StudentBusAssignmentUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.UpdateAsync(id, dto);
                response.Data = data;
                response.Message = ASSIGNMENT_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật student bus assignment theo bus schedule")]
        public async Task<IActionResult> UpdateBySchedule(long id, [FromBody] StudentBusAssignmentByScheduleUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _assignmentService.UpdateByScheduleAsync(id, dto);
                response.Data = data;
                response.Message = ASSIGNMENT_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa student bus assignment")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _assignmentService.DeleteAsync(id);
                response.Message = ASSIGNMENT_DELETE_SUCCESS;
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
