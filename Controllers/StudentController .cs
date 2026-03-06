using BE_API.Dto.Common;
using BE_API.Dto.Student;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;

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
        private const string STUDENT_UPDATE_SUCCESS = "Cập nhật student thành công";
        private const string STUDENT_DELETE_SUCCESS = "Xóa student thành công";

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _studentService.SearchStudentAsync(keyword, page, pageSize);
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

        [HttpPost("[action]")]
        public async Task<IActionResult> Create(StudentCreateDto dto)
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
    }
}
