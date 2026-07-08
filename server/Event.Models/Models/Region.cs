using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    [Table("Management")]
    public class Region
    {
        [Key]
        public string Region_Id { get; set; } = string.Empty;
        
        public int No_Of_Staffs { get; set; }
        
        [Required]
        public string Region_Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<UserInterestedRegion> InterestedUsers { get; set; } = new List<UserInterestedRegion>();
        public virtual ICollection<Staff> Staffs { get; set; } = new List<Staff>();
        public virtual ICollection<Venue> Venues { get; set; } = new List<Venue>();
    }
}
