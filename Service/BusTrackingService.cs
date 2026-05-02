using BE_API.Dto.BusTracking;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusTrackingService : IBusTrackingService
    {
        private readonly IRepository<BusTracking> _busTrackingRepo;
        private readonly IRepository<Bus> _busRepo;

        public BusTrackingService(
            IRepository<BusTracking> busTrackingRepo,
            IRepository<Bus> busRepo)
        {
            _busTrackingRepo = busTrackingRepo;
            _busRepo = busRepo;
        }

        public async Task<BusTrackingDto> UpdateAsync(BusTrackingUpdateDto dto)
        {
            var bus = await ValidateBusAsync(dto.BusId);
            ValidateCoordinates(dto.Latitude, dto.Longitude);
            ValidateSpeed(dto.Speed);

            var tracking = new BusTracking
            {
                BusId = bus.Id,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Speed = dto.Speed,
                TrackedAt = DateTime.UtcNow
            };

            await _busTrackingRepo.AddAsync(tracking);
            await _busTrackingRepo.SaveChangesAsync();

            tracking.Bus = bus;
            return MapToDto(tracking);
        }

        public async Task<BusTrackingDto> GetLatestAsync(long busId)
        {
            await ValidateBusAsync(busId);

            var tracking = await _busTrackingRepo.Get()
                .Include(x => x.Bus)
                .Where(x => x.BusId == busId)
                .OrderByDescending(x => x.TrackedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync()
                ?? throw new Exception("Bus chưa có dữ liệu tracking");

            return MapToDto(tracking);
        }

        private async Task<Bus> ValidateBusAsync(long busId)
        {
            if (busId <= 0)
                throw new Exception("BusId phải lớn hơn 0");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == busId)
                ?? throw new Exception("Bus không tồn tại");

            if (!string.Equals(bus.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Bus đang không hoạt động");

            return bus;
        }

        private static void ValidateCoordinates(double latitude, double longitude)
        {
            if (latitude < -90 || latitude > 90)
                throw new Exception("Latitude không hợp lệ");

            if (longitude < -180 || longitude > 180)
                throw new Exception("Longitude không hợp lệ");
        }

        private static void ValidateSpeed(double? speed)
        {
            if (speed.HasValue && speed.Value < 0)
                throw new Exception("Speed không hợp lệ");
        }

        private static BusTrackingDto MapToDto(BusTracking tracking)
        {
            return new BusTrackingDto
            {
                Id = tracking.Id,
                BusId = tracking.BusId,
                BusLicensePlate = tracking.Bus.LicensePlate,
                Latitude = tracking.Latitude,
                Longitude = tracking.Longitude,
                Speed = tracking.Speed,
                TrackedAt = tracking.TrackedAt
            };
        }
    }
}
