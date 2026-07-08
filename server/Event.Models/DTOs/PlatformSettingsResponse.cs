using System;

namespace Event.Models.DTOs
{
    public class PlatformSettingsResponse
    {
        public decimal Staff_Flat_Rate { get; set; }
        public decimal Virtual_Event_Activation_Fee { get; set; }
        public decimal Physical_Event_Activation_Fee { get; set; }
        public decimal Ticket_Commission_Percentage { get; set; }
        public decimal Ticket_Fixed_Fee { get; set; }
        public int Max_Tickets_Per_Booking { get; set; }
        public decimal GST_Percentage { get; set; }
    }
}
