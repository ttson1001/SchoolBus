using BE_API.Dto.Common;
using BE_API.Dto.Student;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        private const string STUDENT_LIST_SUCCESS = "Lấy danh sách student thành công";
        private const string STUDENT_GET_SUCCESS = "Lấy student thành công";
        private const string STUDENT_CREATE_SUCCESS = "Tạo student thành công";
        private const string STUDENT_IMPORT_SUCCESS = "Import student thành công";
        private const string STUDENT_UPDATE_SUCCESS = "Cập nhật student thành công";
        private const string STUDENT_DELETE_SUCCESS = "Xóa student thành công";

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search(string? keyword, long? campusId, long? guardianId, string? status, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _studentService.SearchStudentAsync(keyword, campusId, guardianId, status, page, pageSize);
                response.Data = data;
                response.Message = STUDENT_LIST_SUCCESS;
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
                var data = await _studentService.GetStudentByIdAsync(id);
                response.Data = data;
                response.Message = STUDENT_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{studentCode}")]
        public async Task<IActionResult> GetByCode(string studentCode)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _studentService.GetStudentByCodeAsync(studentCode);
                response.Data = data;
                response.Message = STUDENT_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{campusId}")]
        public async Task<IActionResult> GetByCampus(long campusId)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _studentService.GetStudentsByCampusIdAsync(campusId);
                response.Data = data;
                response.Message = STUDENT_LIST_SUCCESS;
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
                var data = await _studentService.GetStudentsByGuardianIdAsync(guardianId);
                response.Data = data;
                response.Message = STUDENT_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetByGuardianPhone([FromQuery] string phoneNumber)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _studentService.GetStudentsByGuardianPhoneAsync(phoneNumber);
                response.Data = data;
                response.Message = STUDENT_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetMyStudents()
        {
            var response = new ResponseDto();

            try
            {
                var userId = GetCurrentUserId();
                var role = GetCurrentRole();

                if (!string.Equals(role, "guardian", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Tài khoản hiện tại không phải guardian");

                var data = await _studentService.GetStudentsByGuardianIdAsync(userId);
                response.Data = data;
                response.Message = STUDENT_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] StudentCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                await _studentService.CreateStudentAsync(dto);
                response.Message = STUDENT_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ImportByGuardianEmail([FromForm] StudentImportByGuardianEmailRequestDto dto, CancellationToken cancellationToken)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _studentService.ImportByGuardianEmailAsync(dto, cancellationToken);
                response.Data = data;
                response.Message = STUDENT_IMPORT_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> Update(long id, StudentUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _studentService.UpdateStudentAsync(id, dto);
                response.Data = data;
                response.Message = STUDENT_UPDATE_SUCCESS;
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
                await _studentService.DeleteStudentAsync(id);
                response.Message = STUDENT_DELETE_SUCCESS;
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

            if (string.IsNullOrWhiteSpace(userIdValue) || !long.TryParse(userIdValue, out var userId))
                throw new Exception("Không đọc được UserId từ token");

            return userId;
        }

        private string GetCurrentRole()
        {
            var role = User.FindFirstValue("Role");

            if (string.IsNullOrWhiteSpace(role))
                throw new Exception("Không đọc được Role từ token");

            return role;
        }
    }
}
