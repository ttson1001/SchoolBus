using BE_API.Dto.FaceRecognition;

namespace BE_API.Service.IService
{
    public interface ISystemSettingService
    {
        Task<SimilarityThresholdDto> GetSimilarityThresholdAsync();
        Task<SimilarityThresholdDto> UpdateSimilarityThresholdAsync(decimal similarityThreshold);
        Task<decimal> ResolveSimilarityThresholdAsync(decimal fallbackThreshold);
    }
}
