namespace Event.Models
{
    public class EventStaffAllocation
    {
        public int Event_Id { get; set; }
        public virtual Event Event { get; set; } = null!;

        public int Employee_ID { get; set; }
        public virtual Staff Staff { get; set; } = null!;
    }
}
