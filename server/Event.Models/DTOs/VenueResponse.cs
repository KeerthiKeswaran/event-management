using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class VenueResponse
    {
        public int Venue_Id { get; set; }
        public string Region_Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Hourly_Price { get; set; }
        public bool Is_Available { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SeatTierResponse> SeatTiers { get; set; } = new();
    }

    public class SeatTierResponse
    {
        public string Tier_Name { get; set; } = string.Empty;
        public int Total_Seats { get; set; }
    }
}
