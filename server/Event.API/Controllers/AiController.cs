using System;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IAgentService _agentService;
        private readonly IIntentClassificationService _intentService;
        private readonly IChatHistoryService _chatHistoryService;

        public AiController(IAiService aiService, IAgentService agentService, IIntentClassificationService intentService, IChatHistoryService chatHistoryService)
        {
            _aiService = aiService;
            _agentService = agentService;
            _intentService = intentService;
            _chatHistoryService = chatHistoryService;
        }

        [HttpPost("generate-description")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GenerateDescription([FromBody] AiDescriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { Message = "Prompt cannot be empty." });
            }

            try
            {
                var generatedHtml = await _aiService.GenerateEventDescriptionAsync(request.Prompt);
                return Ok(new { html = generatedHtml });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to generate description.", Details = ex.Message });
            }
        }

        [HttpGet("sessions")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetSessions()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var sessions = await _chatHistoryService.GetChatSessionsAsync(userId);
            return Ok(sessions);
        }

        [HttpGet("chat/sessions")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetSession()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var session = await _chatHistoryService.GetChatSessionAsync(userId, "default");
            if (session == null) return Ok(new { messages = new List<object>() });
            
            return Ok(session);
        }

        [HttpPost("chat/sessions")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> SaveSession([FromBody] ChatSessionDto session)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            session.SessionId = "default";
            await _chatHistoryService.SaveChatSessionAsync(userId, session);
            return Ok(new { success = true });
        }

        [HttpDelete("chat/sessions")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteSession()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            await _chatHistoryService.DeleteChatSessionAsync(userId, "default");
            return Ok(new { success = true });
        }

        [HttpPost("chat")]
        [Authorize(Roles = "User")]
        public async Task ChatStream([FromBody] Event.Models.DTOs.ChatRequestDto request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Response.StatusCode = 401;
                return;
            }

            if (request.Messages == null || request.Messages.Count == 0)
            {
                Response.StatusCode = 400;
                return;
            }

            Response.ContentType = "text/event-stream";

            try
            {
                var lastUserMessage = request.Messages[request.Messages.Count - 1].Content;
                
                await Response.WriteAsync("data: {\"status\": \"Classifying Intent...\"}\n\n");
                await Response.Body.FlushAsync();

                // Intent Classifier
                bool isValid = await _intentService.IsValidEventIntentAsync(lastUserMessage);
                if (!isValid)
                {
                    await Response.WriteAsync("data: {\"response\": \"I am your Event Platform Assistant. I can only help you find events, manage your bookings, and raise support tickets.\"}\n\n");
                    await Response.Body.FlushAsync();
                    return;
                }

                // Call Agent
                var response = await _agentService.ProcessAgentRequestAsync(userId, request.Messages, async (status) => 
                {
                    await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { status = status })}\n\n");
                    await Response.Body.FlushAsync();
                });
                
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { response = response.Response })}\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 500;
                }
                else
                {
                    await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message })}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
        }
    }
}
