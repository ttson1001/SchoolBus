using BE_API.Entites;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Database
{
    public class BeContext : DbContext
    {
        public BeContext(DbContextOptions<BeContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();

        public DbSet<Student> Students => Set<Student>();
        public DbSet<StudentBusAssignment> StudentBusAssignments => Set<StudentBusAssignment>();

        public DbSet<Bus> Buses => Set<Bus>();
        public DbSet<BusStation> BusStations => Set<BusStation>();
        public DbSet<BusRoute> BusRoutes => Set<BusRoute>();
        public DbSet<BusRouteStation> BusRouteStations => Set<BusRouteStation>();
        public DbSet<BusAssignment> BusAssignments => Set<BusAssignment>();

        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<FaceRecognitionLog> FaceRecognitionLogs => Set<FaceRecognitionLog>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<BusTracking> BusTrackings => Set<BusTracking>();

        public DbSet<Campus> Campuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<Order>()
                .HasOne(x => x.Guardian)
                .WithMany()
                .HasForeignKey(x => x.GuardianId)
                .OnDelete(DeleteBehavior.Restrict);

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
