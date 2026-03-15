using BE_API.Dto.Common;
using BE_API.Dto.Package;

namespace BE_API.Service.IService
{
    public interface IPackageService
    {
        Task<PagedResult<PackageDto>> SearchPackageAsync(string? keyword, int page, int pageSize);
        Task<PackageDto> GetPackageByIdAsync(long id);
        Task CreatePackageAsync(PackageCreateDto dto);
        Task<PackageDto> UpdatePackageAsync(long id, PackageUpdateDto dto);
        Task DeletePackageAsync(long id);
    }
}
