using System;

namespace Event.Models.DTOs
{
    public class SupportTicketResponse
    {
        public int Ticket_Id { get; set; }
        public int User_Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string ConcernUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? EsclationStatus { get; set; }
        public int? RelatedId { get; set; }
        public string? TargetType { get; set; }
        public DateTime? Created_At { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public bool IsRaisedByAdmin { get; set; }
    }
}
