using System;
using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class AdminAction
    {
        [Key]
        public int ActionId { get; set; }

        [Required]
        public string AdminId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public virtual Admin Admin { get; set; } = null!;

        [Required]
        public string ActionType { get; set; } = string.Empty; // "REF", "EVT", etc.

        [Required]
        public string TargetType { get; set; } = string.Empty; // "ATD", "ORG"

        [Required]
        public int TargetId { get; set; }

        public int? TicketId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public virtual SupportTicket? SupportTicket { get; set; }

        [Required]
        public string ActionStatus { get; set; } = string.Empty; // "Pending", "Completed", "Failed", etc.

        public string? Remarks { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
