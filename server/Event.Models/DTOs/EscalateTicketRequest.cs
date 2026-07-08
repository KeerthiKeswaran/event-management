namespace Event.Models.DTOs
{
    public class EscalateTicketRequest
    {
        public string ActionType { get; set; } = string.Empty; // REF, EVT, ACC, FIN, GEN
        public string TargetType { get; set; } = string.Empty; // ATD, ORG
        public int TargetId { get; set; } // Attendee/Organizer ID
        public int? TicketId { get; set; } // Event or Booking ID
    }
}
