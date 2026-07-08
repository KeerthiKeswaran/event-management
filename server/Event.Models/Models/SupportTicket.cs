using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class SupportTicket
    {
        [Key]
        public int Ticket_Id { get; set; }
        
        public int User_Id { get; set; }
        public virtual User User { get; set; } = null!;
        
        [Required]
        public string ConcernUrl { get; set; } = string.Empty;

        [Required]
        public string RequestType { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = string.Empty;

        public string? EsclationStatus { get; set; } // "Available", "Unavailable"
        
        public int? RelatedId { get; set; }
        
        public string? TargetType { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
