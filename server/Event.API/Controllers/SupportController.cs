using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Models.DTOs;

namespace Event.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;
        private readonly IUserService _userService;

        public SupportController(ISupportService supportService, IUserService userService)
        {
            _supportService = supportService;
            _userService = userService;
        }

        [HttpPost("tickets")]
        public async Task<IActionResult> SubmitTicket([FromBody] SubmitQueryRequest request)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var success = await _supportService.SubmitSupportTicketAsync(userId, request.Subject, request.Message, request.RequestType, request.RelatedId, request.TargetType);
                if (!success)
                    return BadRequest(new { Message = "Failed to submit support ticket." });

                return Ok(new { Message = "Support ticket submitted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }
        [HttpGet("tickets")]
        public async Task<IActionResult> GetMyTickets()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var tickets = await _supportService.GetMySupportTicketsAsync(userId);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Event.Business.Exceptions.NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
