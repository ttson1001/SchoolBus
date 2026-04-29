namespace BE_API.Common
{
    /// <summary>
    /// Khung giờ mềm không đủ sĩ số tối thiểu: booking đã hủy và phụ huynh đã được thông báo.
    /// </summary>
    public sealed class SoftSlotInsufficientStudentsException : Exception
    {
        public SoftSlotInsufficientStudentsException(string message)
            : base(message)
        {
        }
    }
}
