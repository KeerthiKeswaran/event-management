using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class Venue
    {
        [Key]
        public int Venue_Id { get; set; }
        
        public string Region_Id { get; set; } = string.Empty;
        public virtual Region Region { get; set; } = null!;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Hourly_Price { get; set; }
        
        public bool Is_Available { get; set; }

        // Navigation properties
        public virtual ICollection<VenueSeatCapacity> SeatCapacities { get; set; } = new List<VenueSeatCapacity>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
