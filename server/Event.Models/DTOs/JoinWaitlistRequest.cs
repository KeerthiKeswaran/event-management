using System.ComponentModel.DataAnnotations;

namespace Event.Models.DTOs
{
    public class JoinWaitlistRequest
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        public string TierName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
