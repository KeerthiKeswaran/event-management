using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class EventFeedback
    {
        [Key]
        public int Feedback_Id { get; set; }
        
        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;
        
        public int Attendee_Id { get; set; }
        public virtual User Attendee { get; set; } = null!;
        
        public int Rating { get; set; }
        
        [Required]
        public string Review { get; set; } = string.Empty;
    }
}
