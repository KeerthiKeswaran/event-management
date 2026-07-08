namespace Event.Models.DTOs
{
    public class SubmitFeedbackRequest
    {
        public int Rating { get; set; }
        public string Review { get; set; } = string.Empty;
    }
}
