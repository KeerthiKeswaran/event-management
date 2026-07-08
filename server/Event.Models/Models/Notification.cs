using System;
using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class Notification
    {
        [Key]
        public int Notification_Id { get; set; }

        [Required]
        public string Recipient_Email { get; set; } = string.Empty;

        [Required]
        public string MessageUrl { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending"; // "Pending", "Sent", "Failed"

        public int Retry_Count { get; set; }

        public DateTime Created_At { get; set; } = DateTime.UtcNow;

        public DateTime? Sent_At { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
