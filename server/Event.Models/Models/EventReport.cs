using System;
using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class EventReport
    {
        [Key]
        public int Report_Id { get; set; }
        
        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;
        
        public int Reporter_Id { get; set; }
        public virtual User Reporter { get; set; } = null!;
        
        [Required]
        public string ReportUrl { get; set; } = string.Empty;

        public string? ResponseAction { get; set; } // "Dismissed", "Upholds"
        
        public DateTime Created_At { get; set; }
    }
}
