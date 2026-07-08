namespace Event.Models
{
    public class UserInterestedRegion
    {
        public int User_Id { get; set; }
        public virtual User User { get; set; } = null!;

        public string Region_Id { get; set; } = string.Empty;
        public virtual Region Region { get; set; } = null!;
    }
}
