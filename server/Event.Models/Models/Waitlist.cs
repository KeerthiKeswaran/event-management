using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class Waitlist
    {
        [Key]
        public int Waitlist_Id { get; set; }

        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;

        public int Attendee_Id { get; set; }
        public virtual User Attendee { get; set; } = null!;

        [Required]
        public string Tier_Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Required]
        // "Waiting" | "Notified" | "Booked" | "Expired" | "Cancelled"
        public string Status { get; set; } = "Waiting";

        public int Position { get; set; }

        public DateTime Joined_At { get; set; } = DateTime.UtcNow;

        public DateTime? Notified_At { get; set; }

        public DateTime? Expires_At { get; set; }

        public int? Booking_Id { get; set; }
        public virtual Booking? Booking { get; set; }
    }
}
