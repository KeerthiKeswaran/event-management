using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Event.Models.DTOs
{
    public class CreateTicketTierRequest
    {
        [Required]
        public string TierName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
    }

    public class CreateEventRequest
    {
        [Required]
        public string EventType { get; set; } = string.Empty; // "Physical", "Virtual", "Hybrid"

        [Required]
        [MinLength(3, ErrorMessage = "Title must be at least 3 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty; // E.g., "Music", "Conference", "Tech", "Sports"

        [Required]
        [RegularExpression("^(ALL|KID|ADL)$", ErrorMessage = "Age Category must be one of: ALL (All Ages), KID (Kids), ADL (Adults).")]
        public string AgeCategory { get; set; } = string.Empty; // Must be "ALL" (All Ages), "KID" (Kids), or "ADL" (Adults)

        [Required]
        public string DescriptionUrl { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Range(0.5, 72.0, ErrorMessage = "Duration must be between 30 minutes (0.5) and 72 hours.")]
        public decimal DurationHours { get; set; }

        public bool RequiresStaff { get; set; }

        public int? VenueId { get; set; }

        public string AcceptedPolicyId { get; set; } = string.Empty;

        [Required]
        public List<CreateTicketTierRequest> TicketTiers { get; set; } = new List<CreateTicketTierRequest>();
    }
}
