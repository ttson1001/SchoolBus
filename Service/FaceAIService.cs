using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using BE_API.Configuration;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BE_API.Service
{
    public class FaceAIService : IFaceAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FaceAISettings _settings;
        private readonly ISystemSettingService _systemSettingService;
        private readonly IRepository<Student> _studentRepo;

        public FaceAIService(
            IHttpClientFactory httpClientFactory,
            IOptions<FaceAISettings> settings,
            ISystemSettingService systemSettingService,
            IRepository<Student> studentRepo)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _systemSettingService = systemSettingService;
            _studentRepo = studentRepo;
        }

        public Task<object?> HealthAsync()
        {
            return SendAsync(HttpMethod.Get, "/");
        }

        public Task<object?> CreateStudentAsync(int studentId, string name)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Name không được để trống");

            var path = $"/student?student_id={studentId}&name={Uri.EscapeDataString(name.Trim())}";
            return SendAsync(HttpMethod.Post, path);
        }

        public async Task<object?> AddStudentFaceAsync(int studentId, IFormFile file)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            var studentName = await GetStudentNameAsync(studentId);
            ValidateImageFile(file);

            using var content = new MultipartFormDataContent();
            await AddFileAsync(content, file);

            var path = $"/student/{studentId}/face?name={Uri.EscapeDataString(studentName)}";
            return await SendAsync(HttpMethod.Post, path, content);
        }

        public Task<object?> GetStudentsAsync()
        {
            return SendAsync(HttpMethod.Get, "/students");
        }

        public Task<object?> GetStudentImagesAsync(int studentId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            return SendAsync(HttpMethod.Get, $"/student/{studentId}/images");
        }

        public Task<object?> GetStudentFacesAsync(int studentId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            return SendAsync(HttpMethod.Get, $"/student/{studentId}/faces");
        }

        public Task<object?> DeleteStudentAsync(int studentId)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            return SendAsync(HttpMethod.Delete, $"/student/{studentId}");
        }

        public Task<object?> DeleteFaceAsync(int faceId)
        {
            if (faceId <= 0)
                throw new Exception("FaceId phải lớn hơn 0");

            return SendAsync(HttpMethod.Delete, $"/face/{faceId}");
        }

        public async Task<object?> VerifyStudentAsync(int studentId, IFormFile file)
        {
            if (studentId <= 0)
                throw new Exception("StudentId phải lớn hơn 0");

            var threshold = await ResolveThresholdAsync();
            ValidateImageFile(file);

            using var content = new MultipartFormDataContent();
            await AddFileAsync(content, file);

            return await SendAsync(HttpMethod.Post, $"/verify/{studentId}?threshold={threshold}", content);
        }

        public async Task<object?> VerifyAsync(IFormFile file)
        {
            var threshold = await ResolveThresholdAsync();
            ValidateImageFile(file);

            using var content = new MultipartFormDataContent();
            await AddFileAsync(content, file);

            return await SendAsync(HttpMethod.Post, $"/verify?threshold={threshold}", content);
        }

        public async Task<object?> VerifyTopAsync(IFormFile file, int? topK)
        {
            var threshold = await ResolveThresholdAsync();
            ValidateTopK(topK);
            ValidateImageFile(file);

            using var content = new MultipartFormDataContent();
            await AddFileAsync(content, file);

            var queryParts = new List<string>
            {
                $"threshold={threshold}"
            };

            if (topK.HasValue)
                queryParts.Add($"top_k={topK.Value}");

            return await SendAsync(HttpMethod.Post, $"/verify/top?{string.Join("&", queryParts)}", content);
        }

        private async Task<object?> SendAsync(HttpMethod method, string path, HttpContent? content = null)
        {
            var client = CreateClient();
            using var request = new HttpRequestMessage(method, path)
            {
                Content = content
            };

            using var response = await client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(ExtractErrorMessage(responseText));

            if (string.IsNullOrWhiteSpace(responseText))
                return null;

            var jsonNode = JsonNode.Parse(responseText);
            if (jsonNode == null)
                return null;

            NormalizeUrls(jsonNode);
            return jsonNode;
        }

        private HttpClient CreateClient()
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                throw new Exception("Chưa cấu hình FaceAI:BaseUrl");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/'));
            return client;
        }

        private static async Task AddFileAsync(MultipartFormDataContent content, IFormFile file)
        {
            var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.FileName);
            await Task.CompletedTask;
        }

        private static void ValidateImageFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File ảnh không được để trống");

            if (string.IsNullOrWhiteSpace(file.ContentType) ||
                !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("File tải lên phải là ảnh");
            }
        }

        private static void ValidateThreshold(decimal threshold)
        {
            if (threshold < 0 || threshold > 1)
                throw new Exception("Threshold phải trong khoảng từ 0 đến 1");
        }

        private static void ValidateTopK(int? topK)
        {
            if (!topK.HasValue)
                return;

            if (topK.Value < 1 || topK.Value > 20)
                throw new Exception("TopK phải trong khoảng từ 1 đến 20");
        }

        private async Task<string> GetStudentNameAsync(int studentId)
        {
            var student = await _studentRepo.Get()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == studentId);

            if (student == null)
                throw new Exception("Không tìm thấy học sinh trong hệ thống");

            if (string.IsNullOrWhiteSpace(student.FullName))
                throw new Exception("Học sinh chưa có tên để đồng bộ sang FaceAI");

            return student.FullName.Trim();
        }

        private async Task<decimal> ResolveThresholdAsync()
        {
            var thresholdSetting = await _systemSettingService.GetSimilarityThresholdAsync();
            ValidateThreshold(thresholdSetting.SimilarityThreshold);
            return thresholdSetting.SimilarityThreshold;
        }

        private static string ExtractErrorMessage(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return "FaceAI API trả về lỗi không xác định";

            try
            {
                using var document = JsonDocument.Parse(responseText);
                var root = document.RootElement;

                if (root.TryGetProperty("detail", out var detail))
                    return detail.ToString();

                if (root.TryGetProperty("message", out var message))
                    return message.ToString();
            }
            catch
            {
            }

            return responseText;
        }

        private void NormalizeUrls(JsonNode node)
        {
            if (node is JsonObject jsonObject)
            {
                foreach (var property in jsonObject.ToList())
                {
                    if (property.Value == null)
                        continue;

                    if (property.Value is JsonValue jsonValue &&
                        jsonValue.TryGetValue<string>(out var stringValue) &&
                        IsRelativeUrl(stringValue))
                    {
                        jsonObject[property.Key] = BuildAbsoluteUrl(stringValue);
                        continue;
                    }

                    NormalizeUrls(property.Value);
                }
            }

            if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                {
                    if (item != null)
                        NormalizeUrls(item);
                }
            }
        }

        private static bool IsRelativeUrl(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.StartsWith("/") &&
                   !value.StartsWith("//");
        }

        private string BuildAbsoluteUrl(string relativeUrl)
        {
            return $"{_settings.BaseUrl.TrimEnd('/')}{relativeUrl}";
        }
    }
}
