using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IBookingService
    {
        Task<InitiateBookingResponse?> BookTicketsAsync(int attendeeId, int eventId, Dictionary<string, int> tierQuantities);
        Task<ConfirmBookingResponse?> ConfirmBookingPaymentAsync(int bookingId, string stripeChargeId, string paymentMethod);
        Task<IEnumerable<BookingResponse>> GetMyBookingsAsync(int attendeeId, string? status = null);
        Task<bool> CancelBookingAsync(int bookingId, string refundType = "Dynamic");
        Task ReleaseExpiredEventBookingAsync();
        Task<bool> RevertPendingBookingAsync(int bookingId);
        Task<(DateTime EventDateTime, decimal OriginalAmount)> GetBookingRefundDetailsAsync(int bookingId);
        Task<IEnumerable<ActiveVirtualLinkResponse>> GetActiveVirtualLinksAsync(int attendeeId);
        Task<(bool Success, string SessionId, string ClientSecret, System.DateTime CreatedAtUTC, string ErrorMessage)> CreateCheckoutSessionForBookingAsync(int bookingId, string returnUrl);
        Task<BookingResponse?> CheckInAsync(string qrHash);
    }
}
