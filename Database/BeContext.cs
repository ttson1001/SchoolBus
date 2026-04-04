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
        public DbSet<StudentBusAssignment> StudentBusAssignments => Set<StudentBusAssignment>();

        public DbSet<Bus> Buses => Set<Bus>();
        public DbSet<BusDamageReport> BusDamageReports => Set<BusDamageReport>();
        public DbSet<BusStation> BusStations => Set<BusStation>();
        public DbSet<BusRoute> BusRoutes => Set<BusRoute>();
        public DbSet<BusRouteStation> BusRouteStations => Set<BusRouteStation>();
        public DbSet<BusAssignment> BusAssignments => Set<BusAssignment>();
        public DbSet<BusSchedule> BusSchedules => Set<BusSchedule>();

        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<FaceRecognitionLog> FaceRecognitionLogs => Set<FaceRecognitionLog>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Package> Packages => Set<Package>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<TransactionLog> TransactionLogs => Set<TransactionLog>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<BusTracking> BusTrackings => Set<BusTracking>();

        public DbSet<Campus> Campuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .Property(x => x.DeviceToken)
                .HasMaxLength(1024);

            modelBuilder.Entity<Student>()
                .Property(x => x.AvatarUrl)
                .HasMaxLength(1000);

            modelBuilder.Entity<StudentBusAssignment>()
                .Property(x => x.Note)
                .HasMaxLength(500);

            modelBuilder.Entity<Attendance>()
                .Property(x => x.Note)
                .HasMaxLength(500);

            modelBuilder.Entity<FaceRecognitionLog>()
                .Property(x => x.ConfidenceScore)
                .HasPrecision(10, 6);

            modelBuilder.Entity<Package>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

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

            modelBuilder.Entity<BusAssignment>()
                .HasOne(x => x.Driver)
                .WithMany()
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusAssignment>()
                .HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusAssignment>()
                .HasOne(x => x.Driver)
                .WithMany()
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusAssignment>()
                .HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusDamageReport>()
                .HasOne(x => x.Bus)
                .WithMany(x => x.DamageReports)
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusSchedule>()
                .HasOne(x => x.Bus)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusSchedule>()
                .HasOne(x => x.Route)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<StudentBusAssignment>()
                .HasOne(x => x.PickupStation)
                .WithMany()
                .HasForeignKey(x => x.PickupStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentBusAssignment>()
                .HasOne(x => x.DropOffStation)
                .WithMany()
                .HasForeignKey(x => x.DropOffStationId)
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
        }

    }
}
