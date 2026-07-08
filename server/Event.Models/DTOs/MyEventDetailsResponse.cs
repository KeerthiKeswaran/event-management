using System;
using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class MyEventDetailsResponse
    {
        public int Event_Id { get; set; }
        public int Organizer_Id { get; set; }
        public string Event_Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description_Url { get; set; } = string.Empty;
        public string? Image_Url { get; set; }
        public DateTime Date_Time { get; set; }
        public decimal Duration_Hours { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Requires_Staff { get; set; }
        public int? Venue_Id { get; set; }
        public string? Venue_Name { get; set; }
        public string? Virtual_Url { get; set; }
        public string? Virtual_Password_Hash { get; set; }
        public string? Category { get; set; }
        public int Title_Update_Count { get; set; }
        public List<TicketTierDetailsDto> TicketTiers { get; set; } = new List<TicketTierDetailsDto>();
    }
}
