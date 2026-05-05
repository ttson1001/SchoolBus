using BE_API.Dto.FaceRecognition;
using BE_API.Dto.SystemSetting;

namespace BE_API.Service.IService
{
    public interface ISystemSettingService
    {
        Task<SimilarityThresholdDto> GetSimilarityThresholdAsync();
        Task<SimilarityThresholdDto> UpdateSimilarityThresholdAsync(decimal similarityThreshold);
        Task<decimal> ResolveSimilarityThresholdAsync(decimal fallbackThreshold);
        Task<BookingPickupDistanceSettingDto> GetBookingPickupDistanceMetersAsync();
        Task<BookingPickupDistanceSettingDto> UpdateBookingPickupDistanceMetersAsync(double distanceMeters);
        Task<double> ResolveBookingPickupDistanceMetersAsync(double fallbackDistanceMeters);
    }
}
