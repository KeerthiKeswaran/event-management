using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Event.Models
{
    public class TermsAndConditions
    {
        [Key]
        public string Terms_Id { get; set; } = string.Empty;

        [Required]
        public string Version { get; set; } = string.Empty;

        [Required]
        public string File_Path { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public bool Is_Active { get; set; }

        public DateTime Created_At { get; set; } = DateTime.UtcNow;

        // Navigation property for users who have consented to these terms
        public virtual ICollection<User> ConsentedUsers { get; set; } = new List<User>();
    }
}
