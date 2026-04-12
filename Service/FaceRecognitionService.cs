using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BE_API.Configuration;
using BE_API.Dto.Attendance;
using BE_API.Dto.FaceRecognition;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BE_API.Service
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CompreFaceSettings _settings;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<FaceRecognitionLog> _faceRecognitionLogRepo;
        private readonly IAttendanceService _attendanceService;

        public FaceRecognitionService(
            IHttpClientFactory httpClientFactory,
            IOptions<CompreFaceSettings> settings,
            IRepository<Student> studentRepo,
            IRepository<FaceRecognitionLog> faceRecognitionLogRepo,
            IAttendanceService attendanceService)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _studentRepo = studentRepo;
            _faceRecognitionLogRepo = faceRecognitionLogRepo;
            _attendanceService = attendanceService;
        }

        public async Task<string> CreateSubjectAsync(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new Exception("Subject không được để trống");

            var client = CreateClient();
            var payload = JsonSerializer.Serialize(new { subject = subject.Trim() });
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/recognition/subjects")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Không tạo được subject trên CompreFace: {content}");

            return subject.Trim();
        }

        public async Task<string> RegisterStudentFaceAsync(long studentId, IFormFile file)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == studentId)
                ?? throw new Exception("Student không tồn tại");

            ValidateImageFile(file);

            var subject = BuildStudentSubject(student.Id);
            await EnsureSubjectExistsAsync(subject);

            var client = CreateClient();
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.FileName);

            using var response = await client.PostAsync($"/api/v1/recognition/faces?subject={Uri.EscapeDataString(subject)}", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Không đăng ký khuôn mặt cho học sinh: {responseText}");

            return subject;
        }

        public async Task<FaceRecognitionResultDto> RecognizeStudentAsync(IFormFile file)
        {
            ValidateImageFile(file);

            var responseText = await SendRecognizeRequestAsync(file);
            var result = ParseRecognitionResult(responseText, _settings.SimilarityThreshold);
            await SaveRecognitionLogAsync(result);
            return result;
        }

        public async Task<FaceRecognitionAttendanceResultDto> RecognizeCheckInAsync(FaceRecognitionAttendanceFormDto dto)
        {
            var recognition = await RecognizeStudentAsync(dto.File);

            if (!recognition.IsMatched || !recognition.StudentId.HasValue)
                throw new Exception(recognition.Message ?? "Không nhận diện được học sinh phù hợp");

            var attendance = await _attendanceService.ManualCheckInAsync(new AttendanceManualDto
            {
                StudentId = recognition.StudentId.Value,
                BusId = dto.BusId,
                StationId = dto.StationId,
                Date = dto.Date,
                Time = dto.Time,
                ImageUrl = null
            });

            return new FaceRecognitionAttendanceResultDto
            {
                Recognition = recognition,
                Attendance = attendance
            };
        }

        public async Task<FaceRecognitionAttendanceResultDto> RecognizeCheckOutAsync(FaceRecognitionAttendanceFormDto dto)
        {
            var recognition = await RecognizeStudentAsync(dto.File);

            if (!recognition.IsMatched || !recognition.StudentId.HasValue)
                throw new Exception(recognition.Message ?? "Không nhận diện được học sinh phù hợp");

            var attendance = await _attendanceService.ManualCheckOutAsync(new AttendanceManualDto
            {
                StudentId = recognition.StudentId.Value,
                BusId = dto.BusId,
                StationId = dto.StationId,
                Date = dto.Date,
                Time = dto.Time,
                ImageUrl = null
            });

            return new FaceRecognitionAttendanceResultDto
            {
                Recognition = recognition,
                Attendance = attendance
            };
        }

        private async Task<string> SendRecognizeRequestAsync(IFormFile file)
        {
            var client = CreateClient();
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.FileName);

            using var response = await client.PostAsync("/api/v1/recognition/recognize", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Không nhận diện được khuôn mặt: {responseText}");

            return responseText;
        }

        private async Task SaveRecognitionLogAsync(FaceRecognitionResultDto result)
        {
            if (!result.IsMatched || !result.StudentId.HasValue)
                return;

            var log = new FaceRecognitionLog
            {
                StudentId = result.StudentId.Value,
                ConfidenceScore = result.ConfidenceScore,
                RecognizedAt = DateTime.UtcNow,
                ImageUrl = null
            };

            await _faceRecognitionLogRepo.AddAsync(log);
            await _faceRecognitionLogRepo.SaveChangesAsync();
        }

        private HttpClient CreateClient()
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                throw new Exception("Chưa cấu hình CompreFace:BaseUrl");

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new Exception("Chưa cấu hình CompreFace:ApiKey");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Remove("x-api-key");
            client.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
            return client;
        }

        private async Task EnsureSubjectExistsAsync(string subject)
        {
            try
            {
                await CreateSubjectAsync(subject);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase) &&
                    !ex.Message.Contains("exist", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }
            }
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

        private static string BuildStudentSubject(long studentId)
        {
            return $"student_{studentId}";
        }

        private static FaceRecognitionResultDto ParseRecognitionResult(string responseText, decimal threshold)
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            if (!root.TryGetProperty("result", out var resultArray) ||
                resultArray.ValueKind != JsonValueKind.Array ||
                resultArray.GetArrayLength() == 0)
            {
                return new FaceRecognitionResultDto
                {
                    IsMatched = false,
                    ConfidenceScore = 0,
                    SimilarityThreshold = threshold,
                    Message = "Không tìm thấy khuôn mặt trong ảnh"
                };
            }

            var firstFace = resultArray[0];
            if (!firstFace.TryGetProperty("subjects", out var subjects) ||
                subjects.ValueKind != JsonValueKind.Array ||
                subjects.GetArrayLength() == 0)
            {
                return new FaceRecognitionResultDto
                {
                    IsMatched = false,
                    ConfidenceScore = 0,
                    SimilarityThreshold = threshold,
                    Message = "Không nhận diện được học sinh phù hợp"
                };
            }

            var bestSubject = subjects[0];
            var subject = bestSubject.TryGetProperty("subject", out var subjectElement) ? subjectElement.GetString() : null;
            var similarity = bestSubject.TryGetProperty("similarity", out var similarityElement)
                ? similarityElement.GetDecimal()
                : 0m;

            var matched = !string.IsNullOrWhiteSpace(subject) && similarity >= threshold;
            var studentId = TryParseStudentId(subject);

            return new FaceRecognitionResultDto
            {
                IsMatched = matched && studentId.HasValue,
                StudentId = matched ? studentId : null,
                Subject = subject,
                ConfidenceScore = similarity,
                SimilarityThreshold = threshold,
                Message = matched
                    ? "Nhận diện khuôn mặt thành công"
                    : $"Độ giống {similarity:0.000} chưa đạt ngưỡng {threshold:0.000}"
            };
        }

        private static long? TryParseStudentId(string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return null;

            const string prefix = "student_";
            if (!subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            return long.TryParse(subject[prefix.Length..], out var studentId) ? studentId : null;
        }
    }
}
