namespace Event.Models.DTOs
{
    public class AdminLoginRequest
    {
        public string AdminId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
