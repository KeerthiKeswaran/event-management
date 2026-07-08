using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class Transaction
    {
        [Key]
        public long Transaction_Id { get; set; }
        
        [Required]
        public string Sender_Id { get; set; } = string.Empty;
        
        [Required]
        public string Receiver_Id { get; set; } = string.Empty;
        
        [Required]
        public string Transaction_Type { get; set; } = string.Empty;
        
        public int Related_Id { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public string Currency { get; set; } = string.Empty;
        
        public string? Payment_Method_Details { get; set; }
        
        [Required]
        public string Status { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Refunded_Amount { get; set; }
        
        public string? Remarks { get; set; }
        
        public string? Transaction_Reference { get; set; }
        
        public DateTime Created_At { get; set; }

        // Navigation properties
        public virtual ICollection<BookingPayment> BookingPayments { get; set; } = new List<BookingPayment>();
        public virtual ICollection<OrganizerUpfrontPayment> UpfrontPayments { get; set; } = new List<OrganizerUpfrontPayment>();
        public virtual ICollection<OrganizerPayout> Payouts { get; set; } = new List<OrganizerPayout>();
    }
}
