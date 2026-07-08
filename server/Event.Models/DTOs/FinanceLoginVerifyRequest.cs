namespace Event.Models.DTOs
{
    public class FinanceLoginVerifyRequest
    {
        public string AdminId { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
