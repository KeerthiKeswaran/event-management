using System;

namespace Event.Models.DTOs
{
    public class TicketTierCapacityResponse
    {
        public string Tier_Name { get; set; } = string.Empty;
        public int Total_Seats { get; set; }
        public int Available_Seats { get; set; }
        public int Tickets_Sold { get; set; }
    }
}
