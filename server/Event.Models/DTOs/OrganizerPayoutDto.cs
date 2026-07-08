namespace Event.Models.DTOs
{
    public class OrganizerPayoutDto
    {
        public int Event_Id { get; set; }
        public int Organizer_Id { get; set; }
        public string Organizer_Email { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date_Time { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
