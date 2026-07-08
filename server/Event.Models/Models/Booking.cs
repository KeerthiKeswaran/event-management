using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class Booking
    {
        [Key]
        public int Booking_Id { get; set; }
        
        public int Attendee_Id { get; set; }
        public virtual User Attendee { get; set; } = null!;
        
        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;
        
        [Required]
        public string Booking_Status { get; set; } = string.Empty;
        
        public string? Qr_Code_Path { get; set; }
        
        public string? Qr_Secret_Hash { get; set; }
        
        [Required]
        public string CheckIn_Status { get; set; } = "Pending";
        
        public DateTime Created_At { get; set; }
        
        public string? Virtual_Url { get; set; }

        // Navigation properties
        public virtual ICollection<BookingDetail> Details { get; set; } = new List<BookingDetail>();
        public virtual ICollection<BookingPayment> Payments { get; set; } = new List<BookingPayment>();
    }
}
