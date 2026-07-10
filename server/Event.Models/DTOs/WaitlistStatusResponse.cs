using System;

namespace Event.Models.DTOs
{
    public class WaitlistStatusResponse
    {
        public int Waitlist_Id { get; set; }
        public int Event_Id { get; set; }
        public string Event_Title { get; set; } = string.Empty;
        public string Tier_Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Position { get; set; }
        public DateTime Joined_At { get; set; }
        public DateTime? Expires_At { get; set; }
    }
}
