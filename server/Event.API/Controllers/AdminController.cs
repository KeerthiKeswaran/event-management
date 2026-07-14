using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Business.Exceptions;
using Event.Models.DTOs;

namespace Event.API.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchGlobal([FromQuery] string keyword)
        {
            try
            {
                var result = await _adminService.SearchGlobalAsync(keyword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _adminService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string? keyword,
            [FromQuery] string? eventType,
            [FromQuery] string? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            try
            {
                var result = await _adminService.GetEventsPagedAsync(keyword, eventType, status, startDate, endDate, sortBy, page, size);
                return Ok(result);
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("events/{id:int}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            try
            {
                var ev = await _adminService.GetEventByIdAsync(id);
                if (ev == null) return NotFound(new { Message = "Event not found." });
                return Ok(ev);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("related-entity/{type}/{id:int}")]
        public async Task<IActionResult> GetRelatedEntity(string type, int id)
        {
            try
            {
                var entity = await _adminService.GetRelatedEntityAsync(type, id);
                if (entity == null) return NotFound(new { Message = "Related entity not found." });
                return Ok(entity);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("support/tickets")]
        public async Task<IActionResult> GetSupportTickets(
            [FromQuery] string? status,
            [FromQuery] string? keyword,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            try
            {
                var tickets = await _adminService.GetSupportTicketsAsync(status, keyword, dateFrom, dateTo);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("support/tickets/{id}/respond")]
        public async Task<IActionResult> RespondToTicket(int id, [FromBody] RespondToTicketRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Response))
                {
                    return BadRequest(new { Message = "Response text cannot be empty." });
                }

                var success = await _adminService.RespondToTicketAsync(id, request.Response);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to respond to support ticket." });
                }

                return Ok(new { Message = "Response submitted and user notified successfully." });
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

        [HttpPost("support/tickets/{id}/escalate")]
        public async Task<IActionResult> EscalateTicket(int id, [FromBody] EscalateTicketRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "Request payload cannot be null." });
                }

                // Retrieve current admin ID from JWT claims (NameIdentifier or sub)
                string adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                 ?? User.FindFirst("sub")?.Value 
                                 ?? string.Empty;

                if (string.IsNullOrEmpty(adminId))
                {
                    // Fallback to custom header if available
                    if (HttpContext.Request.Headers.TryGetValue("X-Admin-Id", out var headerId))
                    {
                        adminId = headerId.ToString();
                    }
                }

                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { Message = "Admin identification not found in claims." });
                }

                var success = await _adminService.EscalateTicketAsync(id, adminId, request);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to escalate support ticket." });
                }

                return Ok(new { Message = "Support ticket escalated successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("support/tickets/{id}/escalation-status")]
        public async Task<IActionResult> GetEscalationStatus(int id)
        {
            try
            {
                var action = await _adminService.GetEscalationStatusAsync(id);
                if (action == null)
                {
                    return NotFound(new { Message = "No escalation record found for this ticket." });
                }
                return Ok(new { 
                    ActionId = action.ActionId,
                    ActionStatus = action.ActionStatus,
                    ActionType = action.ActionType,
                    TargetType = action.TargetType,
                    CreatedAt = action.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving escalation status.", Details = ex.Message });
            }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetEventReports()
        {
            try
            {
                var reports = await _adminService.GetFlaggedEventsReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reports/{reportId}/dismiss")]
        public async Task<IActionResult> DismissReport(int reportId)
        {
            try
            {
                var success = await _adminService.DismissEventReportAsync(reportId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to dismiss report." });
                }
                return Ok(new { Message = "Report dismissed successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reports/{reportId}/uphold")]
        public async Task<IActionResult> UpholdReport(int reportId, [FromBody] UpholdReportRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.AdminUpheldMessage))
                {
                    return BadRequest(new { Message = "AdminUpheldMessage is required when upholding a report." });
                }

                string adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                 ?? User.FindFirst("sub")?.Value 
                                 ?? string.Empty;

                if (string.IsNullOrEmpty(adminId))
                {
                    if (HttpContext.Request.Headers.TryGetValue("X-Admin-Id", out var headerId))
                    {
                        adminId = headerId.ToString();
                    }
                }

                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { Message = "Admin identification not found." });
                }

                var success = await _adminService.UpholdEventReportAsync(reportId, adminId, request.AdminUpheldMessage, request.OrganizerAction);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to uphold report." });
                }
                return Ok(new { Message = "Report upheld and organizer status changed successfully." });
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



        [HttpGet("regions")]
        public async Task<IActionResult> GetRegions()
        {
            try
            {
                var regions = await _adminService.GetAllRegionsAsync();
                return Ok(regions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("venues")]
        public async Task<IActionResult> GetVenues()
        {
            try
            {
                var venues = await _adminService.GetAllVenuesIncludingInactiveAsync();
                return Ok(venues);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("venues/search")]
        public async Task<IActionResult> SearchVenues([FromQuery] string name)
        {
            try
            {
                var venues = await _adminService.GetAllVenuesIncludingInactiveAsync();
                if (!string.IsNullOrEmpty(name))
                {
                    venues = venues.Where(v => v.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                return Ok(venues);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("venues/{venueId}")]
        public async Task<IActionResult> UpdateVenue(int venueId, [FromBody] CreateVenueRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { Message = "Request payload cannot be null." });

                var venue = await _adminService.UpdateVenueAsync(venueId, request);
                return Ok(venue);
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

        [HttpPost("venues")]
        public async Task<IActionResult> CreateVenue([FromBody] CreateVenueRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { Message = "Request payload cannot be null." });

                var venue = await _adminService.CreateVenueAsync(request);
                return Ok(venue);
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

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff(
            [FromQuery] string? regionId,
            [FromQuery] bool? isAllocated,
            [FromQuery] string? keyword,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            try
            {
                var directory = await _adminService.GetStaffDirectoryAsync(regionId, isAllocated, keyword, sortBy, page, size);
                return Ok(directory);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("staff/by-region/{regionId}")]
        public async Task<IActionResult> GetStaffByRegion(string regionId)
        {
            try
            {
                var staff = await _adminService.GetStaffByRegionAsync(regionId);
                return Ok(staff);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("events/by-region/{regionId}")]
        public async Task<IActionResult> GetEventsByRegion(string regionId)
        {
            try
            {
                var events = await _adminService.GetEventsByRegionAsync(regionId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("events/{eventId}/allocate-staff")]
        public async Task<IActionResult> AllocateStaff([FromRoute] int eventId, [FromBody] AllocateStaffRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { Message = "Request payload cannot be null." });

                var result = await _adminService.AllocateStaffToEventAsync(eventId, request.EmployeeId);
                return Ok(new { Success = result, Message = "Staff allocated to event successfully." });
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

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                string adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("sub")?.Value
                                 ?? string.Empty;

                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized(new { Message = "Admin identification not found." });

                var profile = await _adminService.GetAdminProfileAsync(adminId);
                return Ok(profile);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { Message = "Name cannot be empty." });

                string adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                 ?? User.FindFirst("sub")?.Value
                                 ?? string.Empty;

                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized(new { Message = "Admin identification not found." });

                var profile = await _adminService.UpdateAdminProfileAsync(adminId, request);
                return Ok(profile);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("venues/all")]
        public async Task<IActionResult> GetAllVenuesIncludingInactive()
        {
            try
            {
                var venues = await _adminService.GetAllVenuesIncludingInactiveAsync();
                return Ok(venues);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("events/{eventId}/venue")]
        public async Task<IActionResult> UpdateEventVenue(int eventId, [FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("venueId", out var venueIdProp))
                    return BadRequest(new { Message = "venueId is required." });

                int venueId = venueIdProp.GetInt32();
                var success = await _adminService.UpdateEventVenueAsync(eventId, venueId);
                if (!success)
                    return BadRequest(new { Message = "Failed to update event venue." });

                return Ok(new { Message = "Event venue updated successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("support/metadata")]
        public async Task<IActionResult> GetHelpdeskMetadata()
        {
            try
            {
                var metadata = await _adminService.GetHelpdeskMetadataAsync();
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? keyword,
            [FromQuery] string? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            try
            {
                var result = await _adminService.GetUsersPagedAsync(keyword, status, startDate, endDate, sortBy, page, size);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                var success = await _adminService.UpdateUserStatusAsync(id, request.Status);
                if (!success)
                    return NotFound(new { Message = "User not found." });

                return Ok(new { Message = "User status updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────
        // GET /api/admin/agent-log  – Returns last 50 lines
        // ─────────────────────────────────────────────────
        [HttpGet("agent-log")]
        public IActionResult GetAgentLog([FromQuery] int lines = 50)
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "agent.log");

                if (!System.IO.File.Exists(logPath))
                    return NotFound(new { Message = "agent.log not found. No agent activity yet.", LogPath = logPath });

                var allLines = System.IO.File.ReadAllLines(logPath);
                var lastLines = allLines.Length <= lines
                    ? allLines
                    : allLines[^lines..];

                return Ok(new
                {
                    TotalLines = allLines.Length,
                    Showing = lastLines.Length,
                    LogPath = logPath,
                    Entries = lastLines
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to read agent.log.", Details = ex.Message });
            }
        }
    }
}
