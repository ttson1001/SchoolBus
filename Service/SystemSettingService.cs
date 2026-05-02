using BE_API.Dto.FaceRecognition;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class SystemSettingService : ISystemSettingService
    {
        private const string SimilarityThresholdKey = "FaceRecognition.SimilarityThreshold";
        private readonly IRepository<SystemSetting> _systemSettingRepo;

        public SystemSettingService(IRepository<SystemSetting> systemSettingRepo)
        {
            _systemSettingRepo = systemSettingRepo;
        }

        public async Task<SimilarityThresholdDto> GetSimilarityThresholdAsync()
        {
            var setting = await _systemSettingRepo.Get()
                .FirstOrDefaultAsync(x => x.Key == SimilarityThresholdKey);

            if (setting == null || !decimal.TryParse(setting.Value, out var threshold))
            {
                throw new Exception("Chưa có SimilarityThreshold trong database");
            }

            return new SimilarityThresholdDto
            {
                SimilarityThreshold = threshold,
                Source = "database"
            };
        }

        public async Task<SimilarityThresholdDto> UpdateSimilarityThresholdAsync(decimal similarityThreshold)
        {
            ValidateSimilarityThreshold(similarityThreshold);

            var normalizedThreshold = Math.Round(similarityThreshold, 4, MidpointRounding.AwayFromZero);
            var setting = await _systemSettingRepo.Get()
                .FirstOrDefaultAsync(x => x.Key == SimilarityThresholdKey);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = SimilarityThresholdKey,
                    Value = normalizedThreshold.ToString("0.####"),
                    Description = "Ngưỡng độ giống cho face recognition"
                };

                await _systemSettingRepo.AddAsync(setting);
            }
            else
            {
                setting.Value = normalizedThreshold.ToString("0.####");
                setting.Description ??= "Ngưỡng độ giống cho face recognition";
                _systemSettingRepo.Update(setting);
            }

            await _systemSettingRepo.SaveChangesAsync();

            return new SimilarityThresholdDto
            {
                SimilarityThreshold = normalizedThreshold,
                Source = "database"
            };
        }

        public async Task<decimal> ResolveSimilarityThresholdAsync(decimal fallbackThreshold)
        {
            var setting = await _systemSettingRepo.Get()
                .FirstOrDefaultAsync(x => x.Key == SimilarityThresholdKey);

            if (setting != null && decimal.TryParse(setting.Value, out var threshold))
                return threshold;

            return fallbackThreshold;
        }

        private static void ValidateSimilarityThreshold(decimal similarityThreshold)
        {
            if (similarityThreshold < 0 || similarityThreshold > 1)
                throw new Exception("SimilarityThreshold phải nằm trong khoảng từ 0 đến 1");
        }
    }
}
