namespace Event.Models
{
    public class VenueSeatCapacity
    {
        public int Venue_Id { get; set; }
        public virtual Venue Venue { get; set; } = null!;

        public string Tier_Name { get; set; } = string.Empty;
        
        public int Total_Seats { get; set; }
    }
}
