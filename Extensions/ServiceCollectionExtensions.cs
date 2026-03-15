using BE_API.Repository;
using BE_API.Service;
using BE_API.Service.IService;

namespace BE_API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void Register(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IBusDamageReportService, BusDamageReportService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IUserService, UserService>();
        }
    }
}
