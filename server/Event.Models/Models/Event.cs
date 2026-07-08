using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class Event
    {
        [Key]
        public int Event_Id { get; set; }

        public int Organizer_Id { get; set; }
        public virtual User Organizer { get; set; } = null!;

        public int? Venue_Id { get; set; }
        public virtual Venue? Venue { get; set; }

        [Required]
        public string Event_Type { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty; // e.g. Tech, Conference, Music, Sports, Workshop, Education, Arts, Food, Wellness

        [Required]
        [MaxLength(3)]
        public string Age_Category { get; set; } = string.Empty; // "ALL", "KID", "ADL"

        public string Description_Url { get; set; } = string.Empty;

        public string? Image_Url { get; set; }

        public DateTime Date_Time { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Duration_Hours { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public bool Requires_Staff { get; set; }

        public string? Virtual_Url { get; set; }

        public string? Virtual_Password_Hash { get; set; }

        public int Title_Update_Count { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<EventTicketTier> TicketTiers { get; set; } = new List<EventTicketTier>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<OrganizerUpfrontPayment> UpfrontPayments { get; set; } = new List<OrganizerUpfrontPayment>();
        public virtual ICollection<OrganizerPayout> Payouts { get; set; } = new List<OrganizerPayout>();
        public virtual ICollection<EventStaffAllocation> StaffAllocations { get; set; } = new List<EventStaffAllocation>();
        public virtual ICollection<EventFeedback> Feedbacks { get; set; } = new List<EventFeedback>();
        public virtual ICollection<EventReport> Reports { get; set; } = new List<EventReport>();
    }
}
