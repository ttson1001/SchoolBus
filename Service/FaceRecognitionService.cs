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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace BE_API.Service
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private const int MaxImageSize = 640;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CompreFaceSettings _settings;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<FaceRecognitionLog> _faceRecognitionLogRepo;
        private readonly IAttendanceService _attendanceService;
        private readonly ICloudinaryService _cloudinaryService;

        public FaceRecognitionService(
            IHttpClientFactory httpClientFactory,
            IOptions<CompreFaceSettings> settings,
            IRepository<Student> studentRepo,
            IRepository<FaceRecognitionLog> faceRecognitionLogRepo,
            IAttendanceService attendanceService,
            ICloudinaryService cloudinaryService)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _studentRepo = studentRepo;
            _faceRecognitionLogRepo = faceRecognitionLogRepo;
            _attendanceService = attendanceService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<string> CreateSubjectAsync(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new Exception("Subject khong duoc de trong");

            var client = CreateClient();
            var payload = JsonSerializer.Serialize(new { subject = subject.Trim() });
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/recognition/subjects")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Khong tao duoc subject tren CompreFace: {content}");

            return subject.Trim();
        }

        public async Task<string> RegisterStudentFaceAsync(long studentId, IFormFile file)
        {
            var student = await _studentRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == studentId)
                ?? throw new Exception("Student khong ton tai");

            ValidateImageFile(file);

            var subject = BuildStudentSubject(student.Id);
            await EnsureSubjectExistsAsync(subject);

            var client = CreateClient();
            using var content = new MultipartFormDataContent();
            await AddImageToFormDataAsync(content, file);

            using var response = await client.PostAsync($"/api/v1/recognition/faces?subject={Uri.EscapeDataString(subject)}", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Khong dang ky khuon mat cho hoc sinh: {responseText}");

            return subject;
        }

        public async Task<FaceSubjectImagesDto> GetSubjectFacesAsync(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new Exception("Subject khong duoc de trong");

            var normalizedSubject = subject.Trim();
            var client = CreateClient();
            var items = new List<FaceSubjectImageDto>();
            var page = 0;
            const int size = 100;
            var totalPages = 1;

            while (page < totalPages)
            {
                using var response = await client.GetAsync($"/api/v1/recognition/faces?page={page}&size={size}&subject={Uri.EscapeDataString(normalizedSubject)}");
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Khong lay duoc danh sach anh cua subject: {responseText}");

                using var document = JsonDocument.Parse(responseText);
                var root = document.RootElement;

                totalPages = ReadInt(root, "total_pages") ?? ReadInt(root, "totalPages") ?? 1;
                if (totalPages <= 0)
                    totalPages = 1;

                if (!root.TryGetProperty("faces", out var facesElement) || facesElement.ValueKind != JsonValueKind.Array)
                {
                    page++;
                    continue;
                }

                foreach (var faceElement in facesElement.EnumerateArray())
                {
                    var imageId = ReadString(faceElement, "image_id") ?? ReadString(faceElement, "imageId");
                    if (string.IsNullOrWhiteSpace(imageId))
                        continue;

                    var imageSubject = ReadString(faceElement, "subject") ?? normalizedSubject;
                    var image = await DownloadSubjectFaceImageAsync(client, _settings.ApiKey, imageId);

                    items.Add(new FaceSubjectImageDto
                    {
                        ImageId = imageId,
                        Subject = imageSubject,
                        ContentType = image.ContentType,
                        ImageBase64 = image.ImageBase64
                    });
                }

                page++;
            }

            return new FaceSubjectImagesDto
            {
                Subject = normalizedSubject,
                TotalItems = items.Count,
                Items = items
            };
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
                throw new Exception(recognition.Message ?? "Khong nhan dien duoc hoc sinh phu hop");

            var imageUrl = await TryUploadAttendanceImageAsync(dto.File, "attendance/checkin");

            var attendance = await _attendanceService.ManualCheckInAsync(new AttendanceManualDto
            {
                StudentId = recognition.StudentId.Value,
                BusId = dto.BusId,
                StationId = dto.StationId,
                Date = dto.Date,
                Time = dto.Time,
                ImageUrl = imageUrl
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
                throw new Exception(recognition.Message ?? "Khong nhan dien duoc hoc sinh phu hop");

            var imageUrl = await TryUploadAttendanceImageAsync(dto.File, "attendance/checkout");

            var attendance = await _attendanceService.ManualCheckOutAsync(new AttendanceManualDto
            {
                StudentId = recognition.StudentId.Value,
                BusId = dto.BusId,
                StationId = dto.StationId,
                Date = dto.Date,
                Time = dto.Time,
                ImageUrl = imageUrl
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
            await AddImageToFormDataAsync(content, file);

            using var response = await client.PostAsync("/api/v1/recognition/recognize", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Khong nhan dien duoc khuon mat: {responseText}");

            return responseText;
        }

        private async Task<string?> TryUploadAttendanceImageAsync(IFormFile file, string folder)
        {
            try
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(file, folder);
                return uploadResult.Url;
            }
            catch
            {
                return null;
            }
        }

        private static async Task AddImageToFormDataAsync(MultipartFormDataContent content, IFormFile file)
        {
            var resized = await ResizeImageIfNeededAsync(file);
            var streamContent = new StreamContent(resized.Stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(resized.ContentType);
            content.Add(streamContent, "file", resized.FileName);
        }

        private static async Task<(MemoryStream Stream, string ContentType, string FileName)> ResizeImageIfNeededAsync(IFormFile file)
        {
            await using var inputStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);

            var width = image.Width;
            var height = image.Height;
            var longestSide = Math.Max(width, height);

            if (longestSide > MaxImageSize)
            {
                var ratio = (double)MaxImageSize / longestSide;
                var resizedWidth = Math.Max(1, (int)Math.Round(width * ratio));
                var resizedHeight = Math.Max(1, (int)Math.Round(height * ratio));

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(resizedWidth, resizedHeight),
                    Mode = ResizeMode.Max
                }));
            }

            var outputStream = new MemoryStream();
            var encoder = GetEncoder(image, file.ContentType, out var contentType, out var fileExtension);
            await image.SaveAsync(outputStream, encoder);
            outputStream.Position = 0;

            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = string.IsNullOrWhiteSpace(originalName) ? "face-image" : originalName;

            return (outputStream, contentType, $"{safeName}{fileExtension}");
        }

        private static IImageEncoder GetEncoder(Image image, string? originalContentType, out string contentType, out string fileExtension)
        {
            var formatName = image.Metadata.DecodedImageFormat?.Name;

            if (string.Equals(formatName, "PNG", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(originalContentType, "image/png", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/png";
                fileExtension = ".png";
                return new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            }

            if (string.Equals(formatName, "BMP", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(originalContentType, "image/bmp", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/bmp";
                fileExtension = ".bmp";
                return new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder();
            }

            if (string.Equals(formatName, "GIF", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(originalContentType, "image/gif", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/gif";
                fileExtension = ".gif";
                return new SixLabors.ImageSharp.Formats.Gif.GifEncoder();
            }

            contentType = "image/jpeg";
            fileExtension = ".jpg";
            return new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder();
        }

        private static async Task<(string? ContentType, string? ImageBase64)> DownloadSubjectFaceImageAsync(HttpClient client, string apiKey, string imageId)
        {
            var candidateEndpoints = new[]
            {
                $"/api/v1/static/{Uri.EscapeDataString(apiKey)}/images/{Uri.EscapeDataString(imageId)}",
                $"/api/v1/recognition/faces/{Uri.EscapeDataString(imageId)}/image",
                $"/api/v1/recognition/faces/{Uri.EscapeDataString(imageId)}"
            };

            foreach (var endpoint in candidateEndpoints)
            {
                using var response = await client.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode)
                    continue;

                var bytes = await response.Content.ReadAsByteArrayAsync();
                if (bytes.Length == 0)
                    continue;

                return (
                    response.Content.Headers.ContentType?.MediaType ?? "image/jpeg",
                    Convert.ToBase64String(bytes)
                );
            }

            return (null, null);
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
                throw new Exception("Chua cau hinh CompreFace:BaseUrl");

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new Exception("Chua cau hinh CompreFace:ApiKey");

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
                throw new Exception("File anh khong duoc de trong");

            if (string.IsNullOrWhiteSpace(file.ContentType) ||
                !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("File tai len phai la anh");
            }
        }

        private static string BuildStudentSubject(long studentId)
        {
            return studentId.ToString();
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
                    Message = "Khong tim thay khuon mat trong anh"
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
                    Message = "Khong nhan dien duoc hoc sinh phu hop"
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
                    ? "Nhan dien khuon mat thanh cong"
                    : $"Do giong {similarity:0.000} chua dat nguong {threshold:0.000}"
            };
        }

        private static int? ReadInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return null;

            return property.TryGetInt32(out var value) ? value : null;
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return null;

            return property.GetString();
        }

        private static long? TryParseStudentId(string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return null;

            const string prefix = "student_";
            if (long.TryParse(subject.Trim(), out var numericStudentId))
                return numericStudentId;

            if (subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(subject[prefix.Length..], out var legacyStudentId))
            {
                return legacyStudentId;
            }

            return null;
        }
    }
}
