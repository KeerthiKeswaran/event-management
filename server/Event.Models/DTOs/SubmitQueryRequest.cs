namespace Event.Models.DTOs
{
    public class SubmitQueryRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public int? RelatedId { get; set; }
        public string TargetType { get; set; } = string.Empty;
    }
}
