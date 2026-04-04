using BE_API.Dto.Upload;
using Microsoft.AspNetCore.Http;

namespace BE_API.Service.IService
{
    public interface ICloudinaryService
    {
        Task<UploadImageResultDto> UploadImageAsync(IFormFile file, string? folder = null);
    }
}
