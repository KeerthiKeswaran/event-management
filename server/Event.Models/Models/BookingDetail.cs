namespace Event.Models
{
    public class BookingDetail
    {
        public int Booking_Id { get; set; }
        public virtual Booking Booking { get; set; } = null!;

        public string Tier_Name { get; set; } = string.Empty;
        
        public int Quantity { get; set; }
    }
}
