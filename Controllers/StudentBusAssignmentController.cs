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

        private const string ASSIGNMENT_GET_SUCCESS = "Lay student bus assignment thanh cong";
        private const string ASSIGNMENT_LIST_SUCCESS = "Lay danh sach student bus assignment thanh cong";
        private const string ASSIGNMENT_CREATE_SUCCESS = "Set diem don tra cho hoc sinh thanh cong";
        private const string ASSIGNMENT_UPDATE_SUCCESS = "Cap nhat student bus assignment thanh cong";
        private const string ASSIGNMENT_DELETE_SUCCESS = "Xoa student bus assignment thanh cong";

        public StudentBusAssignmentController(IStudentBusAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lay student bus assignment theo id")]
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
        [SwaggerOperation(Summary = "Lay assignment theo student")]
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
        [SwaggerOperation(Summary = "Lay assignment theo guardian")]
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
        [SwaggerOperation(Summary = "Set diem don tra theo ngay cho hoc sinh")]
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

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cap nhat student bus assignment")]
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

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xoa student bus assignment")]
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
