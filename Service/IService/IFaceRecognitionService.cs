using BE_API.Dto.FaceRecognition;
using Microsoft.AspNetCore.Http;

namespace BE_API.Service.IService
{
    public interface IFaceRecognitionService
    {
        Task<string> CreateSubjectAsync(string subject);
        Task<string> RegisterStudentFaceAsync(long studentId, IFormFile file);
        Task<FaceRecognitionResultDto> RecognizeStudentAsync(IFormFile file);
    }
}
