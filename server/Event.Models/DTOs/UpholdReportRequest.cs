namespace Event.Models.DTOs
{
    public class UpholdReportRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string OrganizerAction { get; set; } = "No Action"; // "No Action", "Restrict", "Deactivate"
    }
}
