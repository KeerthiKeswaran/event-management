using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class OrganizerUpfrontPayment
    {
        [Key]
        public int Upfront_Payment_Id { get; set; }
        
        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;
        
        public long Transaction_Id { get; set; }
        public virtual Transaction Transaction { get; set; } = null!;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public string Payment_Status { get; set; } = string.Empty;
        
        public DateTime Created_At { get; set; }
    }
}
