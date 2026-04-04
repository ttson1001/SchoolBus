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
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();

            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IBusAssignmentService, BusAssignmentService>();
            services.AddScoped<IBusDamageReportService, BusDamageReportService>();
            services.AddScoped<IBusRouteService, BusRouteService>();
            services.AddScoped<IBusScheduleService, BusScheduleService>();
            services.AddScoped<IBusService, BusService>();
            services.AddScoped<IBusStationService, BusStationService>();
            services.AddScoped<IBusTrackingService, BusTrackingService>();
            services.AddScoped<ICampusService, CampusService>();
            services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IStudentBusAssignmentService, StudentBusAssignmentService>();
            services.AddScoped<ITransactionLogService, TransactionLogService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWalletService, WalletService>();
        }
    }
}
