using BE_API.Dto.BusDamageReport;
using BE_API.Dto.Common;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class BusDamageReportService : IBusDamageReportService
    {
        private readonly IRepository<BusDamageReport> _reportRepo;
        private readonly IRepository<Bus> _busRepo;

        public BusDamageReportService(
            IRepository<BusDamageReport> reportRepo,
            IRepository<Bus> busRepo)
        {
            _reportRepo = reportRepo;
            _busRepo = busRepo;
        }

        public async Task<PagedResult<BusDamageReportDto>> SearchBusDamageReportAsync(string? keyword, string? status, int page, int pageSize)
        {
            var query = _reportRepo.Get()
                .Include(x => x.Bus);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(keyword) ||
                    (x.Description != null && x.Description.ToLower().Contains(keyword)) ||
                    x.Bus.LicensePlate.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.ToLower();
                query = query.Where(x => x.Status.ToLower() == status);
            }

            var totalItems = await query.CountAsync();

            var reports = await query
                .OrderByDescending(x => x.ReportedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BusDamageReportDto>
            {
                Items = reports.Select(MapToDto).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<BusDamageReportDto> GetBusDamageReportByIdAsync(long id)
        {
            var report = await _reportRepo.Get()
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Báo cáo hư hỏng xe không tồn tại");

            return MapToDto(report);
        }

        public async Task CreateBusDamageReportAsync(BusDamageReportCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new Exception("Tiêu đề không được để trống");

            var bus = await _busRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == dto.BusId)
                ?? throw new Exception("Bus không tồn tại");

            var report = new BusDamageReport
            {
                BusId = bus.Id,
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "PENDING" : dto.Status.Trim().ToUpper(),
                ReportedAt = DateTime.UtcNow
            };

            await _reportRepo.AddAsync(report);
            await _reportRepo.SaveChangesAsync();
        }

        public async Task<BusDamageReportDto> UpdateBusDamageReportAsync(long id, BusDamageReportUpdateDto dto)
        {
            var report = await _reportRepo.Get()
                .Include(x => x.Bus)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Báo cáo hư hỏng xe không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                report.Title = dto.Title.Trim();

            if (dto.Description != null)
                report.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Status))
                report.Status = dto.Status.Trim().ToUpper();

            if (dto.ResolvedAt.HasValue)
                report.ResolvedAt = dto.ResolvedAt.Value;

            _reportRepo.Update(report);
            await _reportRepo.SaveChangesAsync();

            return MapToDto(report);
        }

        public async Task DeleteBusDamageReportAsync(long id)
        {
            var report = await _reportRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Báo cáo hư hỏng xe không tồn tại");

            _reportRepo.Delete(report);
            await _reportRepo.SaveChangesAsync();
        }

        private static BusDamageReportDto MapToDto(BusDamageReport report)
        {
            return new BusDamageReportDto
            {
                Id = report.Id,
                BusId = report.BusId,
                BusLicensePlate = report.Bus.LicensePlate,
                Title = report.Title,
                Description = report.Description,
                Status = report.Status,
                ReportedAt = report.ReportedAt,
                ResolvedAt = report.ResolvedAt
            };
        }
    }
}
