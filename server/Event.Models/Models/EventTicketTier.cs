using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class EventTicketTier
    {
        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;

        public string Tier_Name { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public int Tickets_Sold { get; set; }
    }
}
