using BE_API.Configuration;
using BE_API.Dto.Upload;
using BE_API.Service.IService;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BE_API.Service
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;

        public CloudinaryService(IOptions<CloudinarySettings> settings)
        {
            _settings = settings.Value;

            if (string.IsNullOrWhiteSpace(_settings.CloudName) ||
                string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.ApiSecret))
            {
                throw new Exception("Cha cau hAnh Cloudinary");
            }

            var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<UploadImageResultDto> UploadImageAsync(IFormFile file, string? folder = null)
        {
            ValidateImageFile(file);

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = string.IsNullOrWhiteSpace(folder) ? _settings.DefaultFolder : folder.Trim(),
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Upload Cloudinary that bai: {uploadResult.Error.Message}");

            if (uploadResult.SecureUrl == null)
                throw new Exception("Cloudinary khAng tra va URL anh");

            return new UploadImageResultDto
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId ?? string.Empty,
                Format = uploadResult.Format ?? string.Empty,
                Bytes = uploadResult.Bytes
            };
        }

        private static void ValidateImageFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File anh khAng Aac Aa trang");

            if (string.IsNullOrWhiteSpace(file.ContentType) ||
                !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("File tai lAn phai lA anh");
            }
        }
    }
}
