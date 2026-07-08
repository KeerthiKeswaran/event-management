namespace Event.Models.DTOs
{
    public class RegisterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConsentedTermsId { get; set; } = string.Empty;
        public bool HasMarketingConsent { get; set; }
        public string Otp { get; set; } = string.Empty;
    }
}
