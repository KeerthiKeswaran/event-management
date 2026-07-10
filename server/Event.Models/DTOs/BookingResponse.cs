using System;
using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class BookingResponse
    {
        public int Booking_Id { get; set; }
        public int Attendee_Id { get; set; }
        public int Event_Id { get; set; }
        public string Event_Title { get; set; } = string.Empty;
        public string Event_Type { get; set; } = string.Empty;
        public string Event_Venue { get; set; } = string.Empty;
        public DateTime Event_Date_Time { get; set; }
        public string Booking_Status { get; set; } = string.Empty;
        public string? Qr_Code_Path { get; set; }
        public string CheckIn_Status { get; set; } = string.Empty;
        public DateTime Created_At { get; set; }
        public string? Virtual_Url { get; set; }
        public string Event_Status { get; set; } = string.Empty;
        public decimal Amount_Paid { get; set; }
        public decimal Refunded_Amount { get; set; }
        public string? Event_Image_Url { get; set; }
        public List<BookingDetailDto> Details { get; set; } = new List<BookingDetailDto>();
        public bool? Has_Reported { get; set; }
        public int? Feedback_Rating { get; set; }
        public string? Feedback_Review { get; set; }
        public bool? Is_Sold_Out { get; set; }
        public int? Waitlist_Position { get; set; }
    }

    public class BookingDetailDto
    {
        public string Tier_Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class InitiateBookingResponse
    {
        public int Booking_Id { get; set; }
        public int Attendee_Id { get; set; }
        public int Event_Id { get; set; }
        public string Event_Title { get; set; } = string.Empty;
        public string Event_Type { get; set; } = string.Empty;
        public DateTime Event_Date_Time { get; set; }
        public decimal Base_Ticket_Amount { get; set; }
        public decimal Fixed_Fee_Total { get; set; }
        public decimal Gst_Amount { get; set; }
        public decimal Total_Payment { get; set; }
        public decimal Fixed_Fee_Rate { get; set; }
        public decimal Commission_Percentage { get; set; }
    }

    public class ConfirmBookingResponse
    {
        public int Booking_Id { get; set; }
        public int Attendee_Id { get; set; }
        public int Event_Id { get; set; }
        public string Event_Title { get; set; } = string.Empty;
        public string Event_Type { get; set; } = string.Empty;
        public DateTime Event_Date_Time { get; set; }
        public string Qr_Code_Path { get; set; } = string.Empty; // Full absolute path/URL
        public string Virtual_Url { get; set; } = "Disabled"; // Hardcoded to "Disabled" initially
        public string? Event_Image_Url { get; set; }
        public decimal Total_Amount { get; set; }
        public List<ConfirmBookingDetailDto> Details { get; set; } = new List<ConfirmBookingDetailDto>();
    }

    public class ConfirmBookingDetailDto
    {
        public string Tier_Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class ActiveVirtualLinkResponse
    {
        public int Booking_Id { get; set; }
        public int Event_Id { get; set; }
        public string? Virtual_Url { get; set; } = "Disabled";
        public string Link_Status { get; set; } = "Disabled"; // "PendingStart", "Active", "Ended", "NotApplicable"
    }
}
