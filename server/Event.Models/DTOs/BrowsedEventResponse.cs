using System;
using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class BrowsedEventResponse
    {
        public int Event_Id { get; set; }
        public string Organizer_Name { get; set; } = string.Empty;
        public string? Organizer_Email { get; set; }
        public string? Venue_Name { get; set; }
        public string? Address { get; set; }
        public string? Venue_Region_Name { get; set; }
        public string Event_Type { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description_Url { get; set; } = string.Empty;
        public string? Image_Url { get; set; }
        public DateTime Date_Time { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Duration_Hours { get; set; }
        public List<TicketTierDetailsDto> TicketTiers { get; set; } = new List<TicketTierDetailsDto>();
        public List<BrowsedEventReportDto> Reports { get; set; } = new List<BrowsedEventReportDto>();
        public bool? Is_Sold_Out { get; set; }
        public int? Waitlist_Position { get; set; }
    }

    public class BrowsedEventReportDto
    {
        public int Report_Id { get; set; }
        public int Reporter_Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Created_At { get; set; }
    }
}
