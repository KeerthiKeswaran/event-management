using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using Event.Business.Exceptions;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaitlistController : ControllerBase
    {
        private readonly IWaitlistService _waitlistService;
        private readonly IUserService _userService;

        public WaitlistController(IWaitlistService waitlistService, IUserService userService)
        {
            _waitlistService = waitlistService;
            _userService = userService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var result = await _waitlistService.JoinWaitlistAsync(userId, request.EventId, request.TierName, request.Quantity);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ConflictException ex)
            {
                return Conflict(new { Message = ex.Message });
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

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyWaitlist()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var entries = await _waitlistService.GetMyWaitlistAsync(userId);
                return Ok(entries);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{waitlistId}")]
        public async Task<IActionResult> CancelWaitlistEntry(int waitlistId)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var success = await _waitlistService.CancelWaitlistEntryAsync(waitlistId, userId);
                if (success)
                    return Ok(new { Message = "Waitlist entry cancelled successfully." });
                
                return BadRequest(new { Message = "Failed to cancel waitlist entry." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
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

        [Authorize(Roles = "Admin,Organizer")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetEventWaitlist(int eventId)
        {
            try
            {
                var entries = await _waitlistService.GetWaitlistByEventAsync(eventId);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
