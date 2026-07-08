using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using Event.Business.Exceptions;

namespace Event.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IUserService _userService;
        private readonly IRefundService _refundService;

        public BookingController(IBookingService bookingService, IUserService userService, IRefundService refundService)
        {
            _bookingService = bookingService;
            _userService = userService;
            _refundService = refundService;
        }

        [HttpPost]
        public async Task<IActionResult> BookTickets([FromBody] BookTicketsRequest request)
        {
            try
            {
                int attendeeId = _userService.GetCurrentUserId();
                var booking = await _bookingService.BookTicketsAsync(attendeeId, request.EventId, request.TierQuantities);
                if (booking == null)
                    return BadRequest(new { Message = "Booking failed. Capacity exceeded, event not live, or too many tickets requested." });

                return Ok(booking);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("{bookingId}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int bookingId, [FromBody] ConfirmBookingRequest request)
        {
            var booking = await _bookingService.ConfirmBookingPaymentAsync(bookingId, request.StripeChargeId, request.PaymentMethod);
            if (booking == null)
                return BadRequest(new { Message = "Confirm payment failed. Booking not found or not in pending state." });

            return Ok(booking);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyBookings([FromQuery] string? status)
        {
            try
            {
                int attendeeId = _userService.GetCurrentUserId();
                var bookings = await _bookingService.GetMyBookingsAsync(attendeeId, status);
                return Ok(bookings);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("{bookingId}/create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession(int bookingId, [FromBody] CreateCheckoutSessionRequest request)
        {
            var result = await _bookingService.CreateCheckoutSessionForBookingAsync(bookingId, request.SuccessUrl);
            if (!result.Success)
            {
                return BadRequest(new { Message = result.ErrorMessage });
            }

            return Ok(new
            {
                SessionId = result.SessionId,
                ClientSecret = result.ClientSecret,
                CreatedAtUTC = result.CreatedAtUTC
            });
        }

        [HttpPost("{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var success = await _bookingService.CancelBookingAsync(bookingId);
            if (!success)
                return BadRequest(new { Message = "Cancellation failed." });

            return Ok(new { Message = "Booking cancelled successfully." });
        }

        [HttpPost("{bookingId}/revert")]
        public async Task<IActionResult> RevertBooking(int bookingId)
        {
            try
            {
                var success = await _bookingService.RevertPendingBookingAsync(bookingId);
                if (!success)
                    return BadRequest(new { Message = "Booking revert failed." });

                return Ok(new { Message = "Booking reverted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{bookingId}/refund-estimate")]
        public async Task<IActionResult> GetRefundEstimate(int bookingId)
        {
            try
            {
                var (eventDateTime, originalAmount) = await _bookingService.GetBookingRefundDetailsAsync(bookingId);
                var (estimatedRefund, _) = _refundService.CalculateAttendeeRefund(eventDateTime, originalAmount, "Dynamic", 0m);
                return Ok(new { EstimatedRefund = estimatedRefund });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("active-links")]
        public async Task<IActionResult> GetActiveVirtualLinks()
        {
            try
            {
                int attendeeId = _userService.GetCurrentUserId();
                var links = await _bookingService.GetActiveVirtualLinksAsync(attendeeId);
                return Ok(links);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            try
            {
                var booking = await _bookingService.CheckInAsync(request.QrHash);
                return Ok(booking);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }

    public class CheckInRequest
    {
        public string QrHash { get; set; } = string.Empty;
    }
}
