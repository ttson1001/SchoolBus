using BE_API.Entites;
using System.Security.Claims;

namespace BE_API.Service.IService
{
    public interface IJwtService
    {
        string GenerateToken(User account, int? expiresInMinutes);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
