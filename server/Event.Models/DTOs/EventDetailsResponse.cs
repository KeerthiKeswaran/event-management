using System;
using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class EventDetailsResponse
    {
        public int Event_Id { get; set; }
        public int Organizer_Id { get; set; }
        public OrganizerDetailsDto Organizer { get; set; } = null!;
        public VenueDetailsDto? Venue { get; set; }
        public string Event_Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Age_Category { get; set; } = string.Empty;
        public string Description_Url { get; set; } = string.Empty;
        public string? Image_Url { get; set; }
        public DateTime Date_Time { get; set; }
        public decimal Duration_Hours { get; set; }
        public bool? Has_Reported { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<TicketTierDetailsDto> TicketTiers { get; set; } = new List<TicketTierDetailsDto>();
        public string? Virtual_Url { get; set; }
        public string? Virtual_Password_Hash { get; set; }
        public int Title_Update_Count { get; set; }
    }

    public class OrganizerDetailsDto
    {
        public int User_Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class VenueDetailsDto
    {
        public string Region_Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class TicketTierDetailsDto
    {
        public string Tier_Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Tickets_Sold { get; set; }
        public int Capacity { get; set; }
        public bool Has_Active_Waitlist { get; set; }
    }
}
