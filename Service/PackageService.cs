using BE_API.Dto.Common;
using BE_API.Dto.Package;
using BE_API.Entites;
using BE_API.Repository;
using BE_API.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Service
{
    public class PackageService : IPackageService
    {
        private readonly IRepository<Package> _packageRepo;

        public PackageService(IRepository<Package> packageRepo)
        {
            _packageRepo = packageRepo;
        }

        public async Task<PagedResult<PackageDto>> SearchPackageAsync(string? keyword, int page, int pageSize)
        {
            var query = _packageRepo.Get();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(keyword) ||
                    (x.Description != null && x.Description.ToLower().Contains(keyword)) ||
                    (x.Type != null && x.Type.ToLower().Contains(keyword)) ||
                    x.Status.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();

            var packages = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = packages.Select(MapToDto).ToList();

            return new PagedResult<PackageDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PackageDto> GetPackageByIdAsync(long id)
        {
            var package = await _packageRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Package không tồn tại");

            return MapToDto(package);
        }

        public async Task CreatePackageAsync(PackageCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Tên package không được để trống");

            if (string.IsNullOrWhiteSpace(dto.Status))
                throw new Exception("Status không được để trống");

            var package = new Package
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Status = dto.Status.Trim(),
                Type = string.IsNullOrWhiteSpace(dto.Type) ? null : dto.Type.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _packageRepo.AddAsync(package);
            await _packageRepo.SaveChangesAsync();
        }

        public async Task<PackageDto> UpdatePackageAsync(long id, PackageUpdateDto dto)
        {
            var package = await _packageRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Package không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                package.Name = dto.Name.Trim();

            if (dto.Price.HasValue)
                package.Price = dto.Price.Value;

            if (dto.Description != null)
                package.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Status))
                package.Status = dto.Status.Trim();

            if (dto.Type != null)
                package.Type = string.IsNullOrWhiteSpace(dto.Type) ? null : dto.Type.Trim();

            if (dto.ImageUrl != null)
                package.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();

            _packageRepo.Update(package);
            await _packageRepo.SaveChangesAsync();

            return MapToDto(package);
        }

        public async Task DeletePackageAsync(long id)
        {
            var package = await _packageRepo.Get()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Package không tồn tại");

            _packageRepo.Delete(package);
            await _packageRepo.SaveChangesAsync();
        }

        private static PackageDto MapToDto(Package package)
        {
            return new PackageDto
            {
                Id = package.Id,
                Name = package.Name,
                Price = package.Price,
                Description = package.Description,
                Status = package.Status,
                CreatedAt = package.CreatedAt,
                Type = package.Type,
                ImageUrl = package.ImageUrl
            };
        }
    }
}
