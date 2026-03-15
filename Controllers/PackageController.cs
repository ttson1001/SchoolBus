using BE_API.Dto.Common;
using BE_API.Dto.Package;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BE_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackageController : ControllerBase
    {
        private readonly IPackageService _packageService;

        private const string PACKAGE_LIST_SUCCESS = "Lấy danh sách package thành công";
        private const string PACKAGE_GET_SUCCESS = "Lấy package thành công";
        private const string PACKAGE_CREATE_SUCCESS = "Tạo package thành công";
        private const string PACKAGE_UPDATE_SUCCESS = "Cập nhật package thành công";
        private const string PACKAGE_DELETE_SUCCESS = "Xóa package thành công";

        public PackageController(IPackageService packageService)
        {
            _packageService = packageService;
        }

        [HttpGet("[action]")]
        [SwaggerOperation(Summary = "Tìm kiếm package")]
        public async Task<IActionResult> Search(string? keyword, int page = 1, int pageSize = 10)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _packageService.SearchPackageAsync(keyword, page, pageSize);
                response.Data = data;
                response.Message = PACKAGE_LIST_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpGet("[action]/{id}")]
        [SwaggerOperation(Summary = "Lấy package theo id")]
        public async Task<IActionResult> Get(long id)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _packageService.GetPackageByIdAsync(id);
                response.Data = data;
                response.Message = PACKAGE_GET_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "Tạo package")]
        public async Task<IActionResult> Create(PackageCreateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                await _packageService.CreatePackageAsync(dto);
                response.Message = PACKAGE_CREATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpPut("[action]/{id}")]
        [SwaggerOperation(Summary = "Cập nhật package")]
        public async Task<IActionResult> Update(long id, PackageUpdateDto dto)
        {
            var response = new ResponseDto();

            try
            {
                var data = await _packageService.UpdatePackageAsync(id, dto);
                response.Data = data;
                response.Message = PACKAGE_UPDATE_SUCCESS;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return BadRequest(response);
            }
        }

        [HttpDelete("[action]/{id}")]
        [SwaggerOperation(Summary = "Xóa package")]
        public async Task<IActionResult> Delete(long id)
        {
            var response = new ResponseDto();

            try
            {
                await _packageService.DeletePackageAsync(id);
                response.Message = PACKAGE_DELETE_SUCCESS;
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
