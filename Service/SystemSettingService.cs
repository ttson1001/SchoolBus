using BE_API.Dto.FaceRecognition;
using BE_API.Dto.SystemSetting;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class SystemSettingService : ISystemSettingService
    {
        private const string SimilarityThresholdKey = "FaceRecognition.SimilarityThreshold";
        private const string BookingPickupDistanceMetersKey = "Booking.PickupDistanceMeters";
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
                throw new Exception("Chua co SimilarityThreshold trong database");

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
                    Description = "Nguong do giong cho face recognition"
                };

                await _systemSettingRepo.AddAsync(setting);
            }
            else
            {
                setting.Value = normalizedThreshold.ToString("0.####");
                setting.Description ??= "Nguong do giong cho face recognition";
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

        public async Task<double> ResolveBookingPickupDistanceMetersAsync(double fallbackDistanceMeters)
        {
            var setting = await _systemSettingRepo.Get()
                .FirstOrDefaultAsync(x => x.Key == BookingPickupDistanceMetersKey);

            if (setting != null &&
                double.TryParse(setting.Value, out var distanceMeters) &&
                distanceMeters > 0)
            {
                return distanceMeters;
            }

            return fallbackDistanceMeters;
        }

        public async Task<BookingPickupDistanceSettingDto> GetBookingPickupDistanceMetersAsync()
        {
            var setting = await _systemSettingRepo.Get()
                .FirstOrDefaultAsync(x => x.Key == BookingPickupDistanceMetersKey);

            if (setting == null || !double.TryParse(setting.Value, out var distanceMeters) || distanceMeters <= 0)
                throw new Exception("Chua co Booking.PickupDistanceMeters hop le trong database");

            return new BookingPickupDistanceSettingDto
            {
                BookingPickupDistanceMeters = distanceMeters,
                Source = "database"
            };
        }

        public async Task<BookingPickupDistanceSettingDto> UpdateBookingPickupDistanceMetersAsync(double distanceMeters)
        {
            ValidateBookingPickupDistanceMeters(distanceMeters);

            var normalizedDistanceMeters = Math.Round(distanceMeters, 0, MidpointRounding.AwayFromZero);
            var setting = await _systemSettingRepo.Get()
                .FirstOrDefaultAsync(x => x.Key == BookingPickupDistanceMetersKey);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = BookingPickupDistanceMetersKey,
                    Value = normalizedDistanceMeters.ToString("0"),
                    Description = "Khoang cach toi da tu diem don den bus station, tinh theo met"
                };

                await _systemSettingRepo.AddAsync(setting);
            }
            else
            {
                setting.Value = normalizedDistanceMeters.ToString("0");
                setting.Description ??= "Khoang cach toi da tu diem don den bus station, tinh theo met";
                _systemSettingRepo.Update(setting);
            }

            await _systemSettingRepo.SaveChangesAsync();

            return new BookingPickupDistanceSettingDto
            {
                BookingPickupDistanceMeters = normalizedDistanceMeters,
                Source = "database"
            };
        }

        private static void ValidateSimilarityThreshold(decimal similarityThreshold)
        {
            if (similarityThreshold < 0 || similarityThreshold > 1)
                throw new Exception("SimilarityThreshold phai nam trong khoang tu 0 den 1");
        }

        private static void ValidateBookingPickupDistanceMeters(double distanceMeters)
        {
            if (distanceMeters <= 0)
                throw new Exception("BookingPickupDistanceMeters phai lon hon 0");
        }
    }
}
