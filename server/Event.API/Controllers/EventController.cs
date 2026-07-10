using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Event.Contracts.IRepositories;

namespace Event.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IAdminService _adminService;
        private readonly IConfiguration _configuration;

        public EventController(IEventService eventService, IUserService userService, IAdminService adminService, IConfiguration configuration)
        {
            _eventService = eventService;
            _userService = userService;
            _adminService = adminService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var relativePath = _configuration["CategorySettings:CategoriesFilePath"];
            if (string.IsNullOrWhiteSpace(relativePath))
                return NotFound(new { Message = "Categories file path not configured." });

            // Resolve path relative to the Event.Business assembly location
            var assemblyDir = Path.GetDirectoryName(typeof(Event.Business.Services.EventService).Assembly.Location);
            var fullPath = Path.Combine(assemblyDir!, "assets", "events", "categories.json");

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { Message = $"Categories file not found at: {fullPath}" });

            var json = System.IO.File.ReadAllText(fullPath);
            var categories = JsonSerializer.Deserialize<string[]>(json);
            return Ok(categories);
        }

        [AllowAnonymous]
        [HttpGet("age-categories")]
        public IActionResult GetAgeCategories()
        {
            var list = new[]
            {
                new { Key = "ALL", Display = "Unrestricted" },
                new { Key = "KID", Display = "5 years +" },
                new { Key = "ADL", Display = "18+" }
            };
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> BrowseEvents(
            [FromQuery] string? keyword,
            [FromQuery] string? category,
            [FromQuery] DateTime? minDateTime,
            [FromQuery] string? regionId,
            [FromQuery] string? format,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            var result = await _eventService.BrowseEventsAsync(keyword, category, minDateTime, regionId, format, maxPrice, sortBy, page, size);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{eventId}")]
        public async Task<IActionResult> GetEventDetails(int eventId)
        {
            int? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out int id))
                {
                    currentUserId = id;
                }
            }

            var ev = await _eventService.GetEventDetailsAsync(eventId, currentUserId);
            if (ev == null)
                return NotFound(new { Message = "Event not found." });

            var waitlistRepo = HttpContext.RequestServices.GetService<IWaitlistRepository>();
            if (waitlistRepo != null && ev.TicketTiers != null)
            {
                foreach (var tier in ev.TicketTiers)
                {
                    bool hasActive = await waitlistRepo.HasActiveWaitlistAsync(eventId, tier.Tier_Name);
                    if (hasActive && currentUserId.HasValue)
                    {
                        var myWaitlists = await waitlistRepo.GetWaitlistByUserAndEventAsync(currentUserId.Value, eventId);
                        var myNotified = myWaitlists.FirstOrDefault(w => w.Tier_Name.Equals(tier.Tier_Name, StringComparison.OrdinalIgnoreCase) && w.Status == "Notified");
                        if (myNotified != null)
                        {
                            hasActive = false; // For this user, don't show "Waitlist Only" so they can book
                        }
                    }
                    tier.Has_Active_Waitlist = hasActive;
                }
            }

            return Ok(ev);
        }

        [AllowAnonymous]
        [HttpGet("{eventId}/seats")]
        public async Task<IActionResult> GetEventSeats(int eventId)
        {
            try
            {
                var seats = await _eventService.GetEventTicketTierCapacitiesAsync(eventId);
                return Ok(seats);
            }
            catch (Event.Business.Exceptions.NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("venues")]
        public async Task<IActionResult> GetVenues()
        {
            try
            {
                var venues = await _adminService.GetAllVenuesAsync();
                return Ok(venues);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingEvents([FromQuery] int? count)
        {
            try
            {
                var events = await _eventService.GetTrendingEventsAsync(count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularEvents([FromQuery] int regionsLimit = 4)
        {
            try
            {
                var events = await _eventService.GetPopularEventsInCommonAsync(regionsLimit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendedEvents()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var events = await _eventService.GetEventsByInterestedRegionsAsync(userId);
                return Ok(events);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Event.Business.Exceptions.NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{eventId}/details")]
        public async Task<IActionResult> UpdateEventDetails(int eventId, [FromBody] UpdateEventDetailsRequest request)
        {
            try
            {
                int organizerId = _userService.GetCurrentUserId();
                bool result = await _eventService.UpdateEventDetailsAsync(organizerId, eventId, request);
                return Ok(new { Success = result, Message = "Event details updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Event.Business.Exceptions.NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Event.Business.Exceptions.ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/report")]
        public async Task<IActionResult> ReportEvent(int eventId, [FromBody] ReportEventRequest request)
        {
            try
            {
                int reporterId = _userService.GetCurrentUserId();
                var success = await _eventService.ReportEventAsync(reporterId, eventId, request.Reason);
                if (!success)
                    return BadRequest(new { Message = "Failed to report event." });

                return Ok(new { Message = "Event reported successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/feedback")]
        public async Task<IActionResult> SubmitFeedback(int eventId, [FromBody] SubmitFeedbackRequest request)
        {
            try
            {
                int attendeeId = _userService.GetCurrentUserId();
                var success = await _eventService.SubmitEventFeedbackAsync(attendeeId, eventId, request.Rating, request.Review);
                if (!success)
                    return BadRequest(new { Message = "Failed to submit feedback." });

                return Ok(new { Message = "Feedback submitted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("verify-ticket")]
        public async Task<IActionResult> VerifyTicket([FromBody] VerifyTicketRequest request)
        {
            try
            {
                var booking = await _eventService.VerifyTicketCheckInAsync(request.Hash);
                return Ok(new
                {
                    Message = "Ticket verified and checked in successfully.",
                    BookingId = booking.Booking_Id,
                    AttendeeId = booking.Attendee_Id,
                    EventId = booking.Event_Id,
                    CheckInStatus = booking.CheckIn_Status,
                    BookingStatus = booking.Booking_Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                int organizerId = _userService.GetCurrentUserId();
                var createdEvent = await _eventService.CreateEventAsync(organizerId, request);
                return CreatedAtAction(nameof(GetEventDetails), new { eventId = createdEvent.Event_Id }, createdEvent);
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

        [HttpPost("check-staff")]
        public async Task<IActionResult> CheckStaffAvailability([FromBody] CheckStaffAvailabilityRequest request)
        {
            try
            {
                var response = await _eventService.CheckStaffAvailabilityAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession(int eventId, [FromBody] CreateCheckoutSessionRequest request)
        {
            try
            {
                var result = await _eventService.CreateCheckoutSessionForEventCreationAsync(eventId, request.SuccessUrl);
                if (result.Success)
                {
                    return Ok(new { 
                        SessionId = result.SessionId, 
                        ClientSecret = result.ClientSecret, 
                        CreatedAtUTC = result.CreatedAtUTC 
                    });
                }
                return BadRequest(new { Message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/confirm")]
        public async Task<IActionResult> ConfirmEvent(int eventId, [FromBody] ConfirmBookingRequest request)
        {
            try
            {
                var ev = await _eventService.ConfirmEventUpfrontPaymentAsync(eventId, request.StripeChargeId, request.PaymentMethod);
                return Ok(new
                {
                    Message = "Event upfront payment confirmed. Event is now Live.",
                    Event = ev
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("platform-settings")]
        public async Task<IActionResult> GetPlatformSettings()
        {
            try
            {
                var settings = await _eventService.GetPlatformSettingsAsync();
                if (settings == null)
                    return NotFound(new { Message = "Platform settings not configured." });
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("upload-description")]
        public async Task<IActionResult> UploadDescription([FromBody] UploadDescriptionRequest request)
        {
            try
            {
                var url = await _eventService.SaveDescriptionFileAsync(request.Text);
                return Ok(new { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { Message = "No file provided." });

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var url = await _eventService.SaveImageFileAsync(file.FileName, ms.ToArray());
                return Ok(new { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/cancel")]
        public async Task<IActionResult> CancelEvent(int eventId)
        {
            try
            {
                var success = await _eventService.CancelEventAsync(eventId);
                if (!success)
                    return BadRequest(new { Message = "Event cancellation failed." });

                return Ok(new { Message = "Event cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/revert")]
        public async Task<IActionResult> RevertEvent(int eventId)
        {
            try
            {
                var success = await _eventService.RevertPendingEventCreationAsync(eventId);
                if (!success)
                    return BadRequest(new { Message = "Event revert failed." });

                return Ok(new { Message = "Event creation reverted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
