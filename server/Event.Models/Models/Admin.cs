using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class Admin
    {
        [Key]
        public string Admin_Id { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password_Hash { get; set; } = string.Empty;
        
        public string? Password_Reset_Token { get; set; }

        // Navigation properties
        public virtual ICollection<PlatformSettings> UpdatedSettings { get; set; } = new List<PlatformSettings>();
    }
}
