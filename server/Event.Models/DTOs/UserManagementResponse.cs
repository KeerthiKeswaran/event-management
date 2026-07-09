using System;

namespace Event.Models.DTOs
{
    public class UserManagementResponse
    {
        public int User_Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Created_At { get; set; }
        public string Status { get; set; } = string.Empty;
        public int EventsHostedCount { get; set; }
        public int BookingsCount { get; set; }
    }
}
