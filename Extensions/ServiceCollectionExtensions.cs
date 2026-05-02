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
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IBookingRecurringJobService, BookingRecurringJobService>();
            services.AddScoped<IBusRouteService, BusRouteService>();
            services.AddScoped<IBusService, BusService>();
            services.AddScoped<IBusStationService, BusStationService>();
            services.AddScoped<IBusTrackingService, BusTrackingService>();
            services.AddScoped<IBusTripProgressService, BusTripProgressService>();
            services.AddScoped<ICampusService, CampusService>();
            services.AddScoped<IFaceAIService, FaceAIService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<ISystemSettingService, SystemSettingService>();
            services.AddScoped<ITransactionLogService, TransactionLogService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWalletService, WalletService>();
        }
    }
}
