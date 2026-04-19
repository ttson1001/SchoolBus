using BE_API.Dto.FaceRecognition;
using Microsoft.AspNetCore.Http;

namespace BE_API.Service.IService
{
    public interface IFaceAIService
    {
        Task<object?> HealthAsync();
        Task<object?> CreateStudentAsync(int studentId, string name);
        Task<object?> AddStudentFaceAsync(int studentId, IFormFile file);
        Task<object?> GetStudentsAsync();
        Task<object?> GetStudentImagesAsync(int studentId);
        Task<object?> GetStudentFacesAsync(int studentId);
        Task<object?> DeleteStudentAsync(int studentId);
        Task<object?> DeleteFaceAsync(int faceId);
        Task<object?> VerifyStudentAsync(int studentId, IFormFile file);
        Task<object?> VerifyAsync(IFormFile file);
        Task<object?> VerifyTopAsync(IFormFile file, int? topK);
        Task<FaceRecognitionAttendanceResultDto> RecognizeCheckInAsync(FaceRecognitionAttendanceFormDto dto);
        Task<FaceRecognitionAttendanceResultDto> RecognizeCheckOutAsync(FaceRecognitionAttendanceFormDto dto);
    }
}
