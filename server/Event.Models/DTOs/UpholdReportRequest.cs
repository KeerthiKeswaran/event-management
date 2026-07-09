namespace Event.Models.DTOs
{
    public class UpholdReportRequest
    {
        public string AdminUpheldMessage { get; set; } = string.Empty;
        public string OrganizerAction { get; set; } = "No Action"; // "No Action", "Restrict", "Deactivate"
    }
}
