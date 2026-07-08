using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class PlatformSettings
    {
        [Key]
        public int Settings_Id { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Staff_Flat_Rate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Virtual_Event_Activation_Fee { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Physical_Event_Activation_Fee { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Ticket_Commission_Percentage { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Ticket_Fixed_Fee { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GST_Percentage { get; set; } = 18.00m;
        
        public int Max_Tickets_Per_Booking { get; set; }
        
        public DateTime Updated_At { get; set; }
        
        public string Updated_By_Admin_Id { get; set; } = string.Empty;
        public virtual Admin UpdatedByAdmin { get; set; } = null!;
    }
}
