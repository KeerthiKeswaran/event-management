using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class BookTicketsRequest
    {
        public int EventId { get; set; }
        public Dictionary<string, int> TierQuantities { get; set; } = new Dictionary<string, int>();
    }
}
