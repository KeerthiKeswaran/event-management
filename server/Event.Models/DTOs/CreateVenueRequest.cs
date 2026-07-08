using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Event.Models.DTOs
{
    public class CreateVenueRequest
    {
        [Required]
        public string Region_Id { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Hourly price must be greater than zero.")]
        public decimal Hourly_Price { get; set; }

        public bool Is_Available { get; set; } = true;

        [Required]
        public List<SeatTierRequest> SeatTiers { get; set; } = new();
    }

    public class SeatTierRequest
    {
        [Required]
        public string Tier_Name { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Total seats must be at least 1.")]
        public int Total_Seats { get; set; }
    }
}
