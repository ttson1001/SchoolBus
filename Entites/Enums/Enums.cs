namespace BE_API.Entites.Enums
{
    public enum AccountStatus { ACTIVE, DISABLED }
    public enum AttendanceMethod { FACE, MANUAL }
    public enum AttendanceStatus { CHECKED_IN, CHECKED_OUT }
    public enum OrderStatus { PENDING, PAID, CANCELLED, EXPIRED }
    public enum PaymentStatus { SUCCESS, FAILED }
    public enum WalletTopUpStatus { PENDING, PAID, CANCELLED, FAILED }
}
