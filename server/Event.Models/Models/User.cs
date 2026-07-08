using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class User
    {
        [Key]
        public int User_Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Email { get; set; } = string.Empty;
        
        public string Mobile_Number { get; set; } = string.Empty;
        
        [Required]
        public string Password_Hash { get; set; } = string.Empty;
        
        [Required]
        public string Consented_Terms_Id { get; set; } = string.Empty;
        
        public bool Has_Marketing_Consent { get; set; }
        
        public string? Password_Reset_Token { get; set; }
        
        [Required]
        public string Status { get; set; } = "Active"; // "Active", "Restricted", "Deactivated"

        public virtual TermsAndConditions? ConsentedTerms { get; set; }

        // Navigation properties
        public virtual ICollection<UserInterestedRegion> InterestedRegions { get; set; } = new List<UserInterestedRegion>();
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
        public virtual ICollection<EventFeedback> Feedbacks { get; set; } = new List<EventFeedback>();
        public virtual ICollection<EventReport> Reports { get; set; } = new List<EventReport>();
    }
}
