namespace Event.Models.DTOs
{
    public class StaffResponse
    {
        public int Employee_ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Region_Id { get; set; } = string.Empty;
        public string? Region_Name { get; set; }
        public bool IsAllocated { get; set; }
    }
}
