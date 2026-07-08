namespace Event.Models.DTOs
{
    public class CloseAccountRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string ConfirmName { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
