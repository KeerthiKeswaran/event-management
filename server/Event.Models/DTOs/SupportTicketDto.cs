using System;

namespace Event.Models.DTOs
{
    public class SupportTicketDto
    {
        public string TicketId { get; set; } = string.Empty;
        public string BookingId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? Response { get; set; }
    }
}
