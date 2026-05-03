using BE_API.Entites;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Database
{
    public class BeContext : DbContext
    {
        public BeContext(DbContextOptions<BeContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<Role> Roles => Set<Role>();

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Booking> Bookings => Set<Booking>();

        public DbSet<Bus> Buses => Set<Bus>();
        public DbSet<BusStation> BusStations => Set<BusStation>();
        public DbSet<BusRoute> BusRoutes => Set<BusRoute>();
        public DbSet<BusRouteStation> BusRouteStations => Set<BusRouteStation>();
        public DbSet<BusTripProgress> BusTripProgresses => Set<BusTripProgress>();
        public DbSet<BusRun> BusRuns => Set<BusRun>();
        public DbSet<BusRunStudent> BusRunStudents => Set<BusRunStudent>();

        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<FaceRecognitionLog> FaceRecognitionLogs => Set<FaceRecognitionLog>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Package> Packages => Set<Package>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<TransactionLog> TransactionLogs => Set<TransactionLog>();

        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

        public DbSet<BusTracking> BusTrackings => Set<BusTracking>();

        public DbSet<Campus> Campuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .Property(x => x.DeviceToken)
                .HasMaxLength(1024);

            modelBuilder.Entity<User>()
                .Property(x => x.AvatarUrl)
                .HasMaxLength(1000);

            modelBuilder.Entity<SystemSetting>()
                .Property(x => x.Key)
                .HasMaxLength(200);

            modelBuilder.Entity<SystemSetting>()
                .Property(x => x.Value)
                .HasMaxLength(200);

            modelBuilder.Entity<SystemSetting>()
                .Property(x => x.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<SystemSetting>()
                .HasIndex(x => x.Key)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .Property(x => x.AvatarUrl)
                .HasMaxLength(1000);

            modelBuilder.Entity<Booking>()
                .Property(x => x.Status)
                .HasMaxLength(50);

            modelBuilder.Entity<Booking>()
                .Property(x => x.Note)
                .HasMaxLength(500);

            modelBuilder.Entity<Booking>()
                .Property(x => x.PickupAddress)
                .HasMaxLength(500);

            modelBuilder.Entity<BusRun>()
                .Property(x => x.Status)
                .HasMaxLength(50);

            modelBuilder.Entity<Attendance>()
                .Property(x => x.Note)
                .HasMaxLength(500);

            modelBuilder.Entity<Attendance>()
                .Property(x => x.CheckInImageUrl)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<Attendance>()
                .Property(x => x.CheckOutImageUrl)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<FaceRecognitionLog>()
                .Property(x => x.ConfidenceScore)
                .HasPrecision(10, 6);

            modelBuilder.Entity<Package>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(x => x.SelectedRouteIds)
                .HasMaxLength(500);

            modelBuilder.Entity<Payment>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TransactionLog>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TransactionLog>()
                .Property(x => x.OldBalance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TransactionLog>()
                .Property(x => x.NewBalance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Wallet>()
                .Property(x => x.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BusTripProgress>()
                .HasOne(x => x.Bus)
                .WithMany()
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusTripProgress>()
                .HasOne(x => x.BusRun)
                .WithMany()
                .HasForeignKey(x => x.BusRunId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusTripProgress>()
                .HasOne(x => x.Route)
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusTripProgress>()
                .HasOne(x => x.Station)
                .WithMany()
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusTripProgress>()
                .HasIndex(x => new { x.BusRunId, x.RideDate, x.OrderIndex })
                .IsUnique();

            modelBuilder.Entity<BusRun>()
                .HasOne(x => x.Route)
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusRun>()
                .HasOne(x => x.Bus)
                .WithMany()
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusRun>()
                .HasOne(x => x.Driver)
                .WithMany()
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusRun>()
                .HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusRun>()
                .HasIndex(x => new { x.RouteId, x.ServiceDate, x.StartTime, x.RunOrder })
                .IsUnique();

            modelBuilder.Entity<BusRunStudent>()
                .HasOne(x => x.BusRun)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.BusRunId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusRunStudent>()
                .HasOne(x => x.Booking)
                .WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusRunStudent>()
                .HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusRunStudent>()
                .HasIndex(x => x.BookingId)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasOne(x => x.Guardian)
                .WithMany()
                .HasForeignKey(x => x.GuardianId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(x => x.Package)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(x => x.Route)
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(x => x.Station)
                .WithMany()
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(x => x.CheckInStation)
                .WithMany()
                .HasForeignKey(x => x.CheckInStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(x => x.CheckOutStation)
                .WithMany()
                .HasForeignKey(x => x.CheckOutStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionLog>()
                .HasOne(x => x.Order)
                .WithMany(x => x.TransactionLogs)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<Wallet>()
                .HasOne(x => x.User)
                .WithOne(x => x.Wallet)
                .HasForeignKey<Wallet>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Campus)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusRoute>()
                .HasOne(r => r.Campus)
                .WithMany(c => c.BusRoutes)
                .HasForeignKey(r => r.CampusId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusStation>()
                .HasOne(x => x.Campus)
                .WithMany(x => x.BusStations)
                .HasForeignKey(x => x.CampusId)
                .OnDelete(DeleteBehavior.SetNull);
        }

    }
}
