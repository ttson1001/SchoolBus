using BE_API.Entites.Enums;

namespace BE_API.Entites
{
    public class Order : IEntity
    {
        public long Id { get; set; }

        public long GuardianId { get; set; }
        public User Guardian { get; set; } = null!;

        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public long BusRouteId { get; set; }
        public BusRoute BusRoute { get; set; } = null!;

        public long PackageId { get; set; }
        public Package Package { get; set; } = null!;

        public OrderStatus Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TransactionLog> TransactionLogs { get; set; } = new List<TransactionLog>();
    }
}
