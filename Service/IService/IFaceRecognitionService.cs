using BE_API.Dto.FaceRecognition;
using Microsoft.AspNetCore.Http;

namespace BE_API.Service.IService
{
    public interface IFaceRecognitionService
    {
        Task<string> CreateSubjectAsync(string subject);
        Task<string> RegisterStudentFaceAsync(long studentId, IFormFile file);
        Task<FaceSubjectImagesDto> GetSubjectFacesAsync(string subject);
        Task<FaceRecognitionResultDto> RecognizeStudentAsync(IFormFile file);
        Task<FaceRecognitionAttendanceResultDto> RecognizeCheckInAsync(FaceRecognitionAttendanceFormDto dto);
        Task<FaceRecognitionAttendanceResultDto> RecognizeCheckOutAsync(FaceRecognitionAttendanceFormDto dto);
    }
}
