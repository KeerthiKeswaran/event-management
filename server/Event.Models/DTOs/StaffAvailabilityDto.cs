using System;
using System.ComponentModel.DataAnnotations;

namespace Event.Models.DTOs
{
    public class CheckStaffAvailabilityRequest
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        [Range(1, 168)]
        public int DurationHours { get; set; }
    }

    public class StaffAvailabilityResponse
    {
        public int RequiredStaffCount { get; set; }
        public int AvailableStaffCount { get; set; }
        public decimal StaffingCost { get; set; }
        public bool IsAdequate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
