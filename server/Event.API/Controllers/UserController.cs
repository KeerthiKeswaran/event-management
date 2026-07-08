using System;
using System.Linq;
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
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("select-regions")]
        public async Task<IActionResult> SelectRegions([FromBody] SelectRegionsRequest request)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var success = await _userService.SelectInterestedRegionsAsync(userId, request.RegionId);
                if (!success)
                    return BadRequest(new { Message = "Failed to update interested regions." });

                return Ok(new { Message = "Interested regions updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var profile = await _userService.GetUserProfileAsync(userId);
                if (profile == null)
                    return NotFound(new { Message = "User profile not found." });

                return Ok(profile);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var success = await _userService.UpdateUserProfileAsync(userId, request);
                if (!success)
                    return BadRequest(new { Message = "Failed to update user profile." });

                return Ok(new { Message = "Profile updated successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (ConflictException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpGet("my-events")]
        public async Task<IActionResult> GetMyEvents()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var events = await _userService.GetMyEventsAsync(userId);
                return Ok(events);
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

        [HttpGet("my-dashboard")]
        public async Task<IActionResult> GetMyDashboard()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var events = (await _userService.GetMyEventsAsync(userId)).ToList();

                int totalEvents = events.Count;
                int ticketsSold = events.Sum(e => e.Tickets_Sold);
                decimal netEarnings = events.Sum(e => e.Net_Earnings);

                // upcoming: events starting in the future with Live/Pending status (up to 3 for preview)
                var now = DateTime.UtcNow;
                var upcoming = events
                    .Where(e => e.Date_Time > now && (e.Status == "Live" || e.Status == "Pending"))
                    .OrderBy(e => e.Date_Time)
                    .Take(3)
                    .ToList();

                return Ok(new
                {
                    TotalEvents = totalEvents,
                    TicketsSold = ticketsSold,
                    NetEarnings = netEarnings,
                    UpcomingEvents = upcoming
                });
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

        [HttpGet("my-events/{eventId}")]
        public async Task<IActionResult> ViewMyEvent(int eventId)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var ev = await _userService.GetMyEventDetailsAsync(userId, eventId);
                if (ev == null)
                    return NotFound(new { Message = "Event not found." });

                return Ok(ev);
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

        [HttpPost("close-account")]
        public async Task<IActionResult> CloseAccount([FromBody] CloseAccountRequest request)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var success = await _userService.CloseAccountAsync(userId, request);
                if (!success)
                    return BadRequest(new { Message = "Failed to close account." });

                return Ok(new { Message = "Account closed successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message });
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
}
