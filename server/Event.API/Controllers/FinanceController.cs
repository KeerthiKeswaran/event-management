using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Business.Exceptions;
using Event.Models.DTOs;

namespace Event.API.Controllers
{
    [Authorize(Roles = "finance")]
    [ApiController]
    [Route("api/finance")]
    public class FinanceController : ControllerBase
    {
        private readonly IFinanceService _financeService;

        public FinanceController(IFinanceService financeService)
        {
            _financeService = financeService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchGlobal([FromQuery] string keyword, [FromServices] IAdminService adminService)
        {
            try
            {
                var result = await adminService.SearchGlobalAsync(keyword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("related-entity/{type}/{id:int}")]
        public async Task<IActionResult> GetRelatedEntity(string type, int id, [FromServices] IAdminService adminService)
        {
            try
            {
                var entity = await adminService.GetRelatedEntityAsync(type, id);
                if (entity == null) return NotFound(new { Message = "Related entity not found." });
                return Ok(entity);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("actions")]
        public async Task<IActionResult> GetAdminActions()
        {
            try
            {
                var actions = await _financeService.GetAdminActionsAsync();
                return Ok(actions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("actions/{id}/decline")]
        public async Task<IActionResult> DeclineAction(int id, [FromBody] DeclineActionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "Request body cannot be null." });
                }

                var success = await _financeService.DeclineActionAsync(id, request.Remarks);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to decline admin action." });
                }

                return Ok(new { Message = "Admin action declined successfully." });
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

        [HttpPost("actions/{id}/approve")]
        public async Task<IActionResult> ApproveAction(int id, [FromBody] ApproveActionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.RefundType))
                {
                    return BadRequest(new { Message = "Refund type is required." });
                }

                var success = await _financeService.ApproveActionAsync(id, request.RefundType, request.Message);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to approve admin action." });
                }

                return Ok(new { Message = "Admin action approved and refund processed successfully." });
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

        [HttpPost("tickets/{id}/respond")]
        public async Task<IActionResult> RespondToTicket(int id, [FromBody] RespondToTicketRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Response))
                {
                    return BadRequest(new { Message = "Response text cannot be empty." });
                }

                var success = await _financeService.RespondToTicketAsync(id, request.Response);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to submit response." });
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

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string? keyword,
            [FromQuery] string? transactionType,
            [FromQuery] string? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            try
            {
                var result = await _financeService.GetTransactionsPagedAsync(
                    keyword,
                    transactionType,
                    status,
                    startDate,
                    endDate,
                    sortBy,
                    page,
                    size);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _financeService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("payouts")]
        public async Task<IActionResult> GetOrganizerPayouts(
            [FromQuery] string? status,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            try
            {
                var payouts = await _financeService.GetOrganizerPayoutsPagedAsync(status, sortBy, page, size);
                return Ok(payouts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
