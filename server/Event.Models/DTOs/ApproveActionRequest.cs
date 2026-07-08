namespace Event.Models.DTOs
{
    public class ApproveActionRequest
    {
        public string RefundType { get; set; } = string.Empty; // "FUL", "DYN", "REM", "NOR"
        public string Message { get; set; } = string.Empty; // Dynamic messaging for cancellation
    }
}
