using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class OrganizerPayout
    {
        [Key]
        public int Payout_Id { get; set; }
        
        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;
        
        public long? Transaction_Id { get; set; }
        public virtual Transaction? Transaction { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total_Ticket_Sales { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Platform_Commission { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Payout_Amount { get; set; }
        
        [Required]
        public string Payout_Status { get; set; } = string.Empty;
        
        public DateTime Processed_At { get; set; }
    }
}
