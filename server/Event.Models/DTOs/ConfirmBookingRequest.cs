namespace Event.Models.DTOs
{
    public class ConfirmBookingRequest
    {
        public string StripeChargeId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
