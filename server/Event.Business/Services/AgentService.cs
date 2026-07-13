using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Models.DTOs;
using Event.Contracts.IServices;
using Microsoft.Extensions.Configuration;

namespace Event.Business.Services
{
    public class AgentService : IAgentService
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _agentModel;
        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;
        private readonly ISupportService _supportService;
        private readonly string _logFilePath;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };

        #endregion

        #region Constructor

        public AgentService(
            HttpClient httpClient, 
            IConfiguration configuration,
            IEventService eventService,
            IBookingService bookingService,
            ISupportService supportService)
        {
            _httpClient = httpClient;
            var groqSection = configuration.GetSection("Groq");
            _apiKey = groqSection["ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is missing.");
            _baseUrl = groqSection["BaseUrl"] ?? throw new InvalidOperationException("Groq:Url is missing.");
            _agentModel = groqSection["AgentModel"] ?? throw new InvalidOperationException("Groq:Model Configuration file is missing.");
            
            _eventService = eventService;
            _bookingService = bookingService;
            _supportService = supportService;
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "agent.log");
        }

        private async Task LogActivityAsync(string level, string message)
        {
            try
            {
                string logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] [{level}] {message}\n";
                await File.AppendAllTextAsync(_logFilePath, logEntry);
            }
            catch { /* Ignore logging errors to prevent crashing */ }
        }

        #endregion

        #region GetAvailableTools

        private List<ToolDefinitionDto> GetAvailableTools()
        {
            return new List<ToolDefinitionDto>
            {
                new ToolDefinitionDto
                {
                    Function = new FunctionDefinitionDto
                    {
                        Name = "SearchEvents",
                        Description = "Search for upcoming events. Provide a keyword, and optionally a category or date. Always return results as HTML formatted cards with a clickable link like <a href='/event/{EventId}'>View Event</a>.",
                        Parameters = new {
                            type = "object",
                            properties = new {
                                keyword = new { type = "string", description = "Search query for the event name or description" },
                                category = new { type = "string", description = "Category of the event (e.g. Music, Tech)" }
                            }
                        }
                    }
                },
                new ToolDefinitionDto
                {
                    Function = new FunctionDefinitionDto
                    {
                        Name = "GetMyBookings",
                        Description = "Retrieve all the user's bookings, including upcoming, past, and cancelled tickets.",
                        Parameters = new { type = "object", properties = new {} }
                    }
                },
                new ToolDefinitionDto
                {
                    Function = new FunctionDefinitionDto
                    {
                        Name = "CancelBooking",
                        Description = "Cancel a specific booking ID for the user.",
                        Parameters = new {
                            type = "object",
                            properties = new {
                                bookingId = new { type = "integer", description = "The ID of the booking to cancel" }
                            },
                            required = new[] { "bookingId" }
                        }
                    }
                },
                new ToolDefinitionDto
                {
                    Function = new FunctionDefinitionDto
                    {
                        Name = "GetMySupportTickets",
                        Description = "Retrieve all the user's support tickets, including active and closed, to check their status.",
                        Parameters = new { type = "object", properties = new {} }
                    }
                },
                new ToolDefinitionDto
                {
                    Function = new FunctionDefinitionDto
                    {
                        Name = "RaiseSupportTicket",
                        Description = "Submit a new support ticket for the user.",
                        Parameters = new {
                            type = "object",
                            properties = new {
                                subject = new { type = "string", description = "Summary of the issue" },
                                message = new { type = "string", description = "Detailed description of the problem" },
                                category = new { type = "string", description = "Category of the ticket ('REF' for Refund, 'GEN' for General)" },
                                relatedId = new { type = "string", description = "The ID of the related booking or event (as a string)" },
                                targetType = new { type = "string", description = "'ATD' if related to a booking, 'ORG' if related to an event" }
                            },
                            required = new[] { "subject", "message", "category", "relatedId", "targetType" }
                        }
                    }
                },
                new ToolDefinitionDto
                {
                    Function = new FunctionDefinitionDto
                    {
                        Name = "GetPlatformNavigation",
                        Description = "Retrieve step-by-step navigation instructions for platform actions (e.g. how to create an event, book, cancel, or contact support).",
                        Parameters = new {
                            type = "object",
                            properties = new {
                                actionName = new { type = "string", description = "The action the user wants to perform (e.g. 'create_event', 'book_event', 'cancel_booking', 'contact_support')" }
                            },
                            required = new[] { "actionName" }
                        }
                    }
                }
            };
        }

        #endregion

        #region ProcessAgentRequestAsync

        public async Task<ChatResponseDto> ProcessAgentRequestAsync(string userId, List<ChatMessageDto> messages, Action<string>? onProgress = null)
        {
            await LogActivityAsync("INFO", $"Processing Agent Request for User {userId}. Message Count: {messages.Count}");
            int numericUserId = int.Parse(userId);
            var tools = GetAvailableTools();

            string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (rootPath.Contains("bin"))
            {
                rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            
            string promptPath = Path.Combine(rootPath, "Event.Business", "assets", "agents", "agent-prompt.txt");
            string systemPrompt = await File.ReadAllTextAsync(promptPath);

            var systemMessage = new ChatMessageDto
            {
                Role = "system",
                Content = systemPrompt
            };

            var conversation = new List<ChatMessageDto> { systemMessage };
            conversation.AddRange(messages);

            int maxLoops = 5;
            for (int i = 0; i < maxLoops; i++)
            {
                if (i == 0) onProgress?.Invoke("Thinking...");

                await LogActivityAsync("INFO", $"[Iteration {i+1}/{maxLoops}] Sending request to Groq API...");

                var requestBody = new GroqChatRequest
                {
                    Model = _agentModel,
                    Messages = conversation,
                    Tools = tools
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_baseUrl, jsonContent);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = $"Groq API HTTP Error {response.StatusCode}: {responseJson}";
                    await LogActivityAsync("ERROR", errorMsg);
                    return new ChatResponseDto { Response = "I'm sorry, I'm having trouble connecting to my servers right now." };
                }

                await LogActivityAsync("SUCCESS", $"Received successful response from Groq API.");

                var groqResponse = JsonSerializer.Deserialize<GroqChatResponse>(responseJson, _jsonOptions);
                var choice = groqResponse?.Choices?[0];
                var responseMessage = choice?.Message;

                if (responseMessage == null) break;

                conversation.Add(responseMessage);

                if (choice.FinishReason == "tool_calls" && responseMessage.ToolCalls != null)
                {
                    await LogActivityAsync("INFO", $"LLM requested {responseMessage.ToolCalls.Count} tool calls.");
                    foreach (var toolCall in responseMessage.ToolCalls)
                    {
                        onProgress?.Invoke($"Executing {toolCall.Function.Name}...");
                        await LogActivityAsync("INFO", $"Executing Tool: {toolCall.Function.Name}");
                        string functionResult = await ExecuteToolAsync(numericUserId, toolCall.Function);
                        await LogActivityAsync("INFO", $"Tool {toolCall.Function.Name} execution completed.");
                        conversation.Add(new ChatMessageDto
                        {
                            Role = "tool",
                            ToolCallId = toolCall.Id,
                            Content = functionResult
                        });
                    }
                    // Loop again to send the tool results back to the model
                    continue;
                }
                
                // If finish_reason is stop or no tool calls, return final response
                await LogActivityAsync("SUCCESS", "Final response generated successfully.");
                return new ChatResponseDto { Response = responseMessage.Content ?? "" };
            }

            await LogActivityAsync("WARNING", "Max loops reached without final resolution.");
            return new ChatResponseDto { Response = "I'm sorry, I couldn't complete the task in time." };
        }

        #endregion

        #region ExecuteToolAsync

        private async Task<string> ExecuteToolAsync(int userId, ToolCallFunctionDto function)
        {
            try
            {
                var args = string.IsNullOrEmpty(function.Arguments) ? new JsonElement() : JsonSerializer.Deserialize<JsonElement>(function.Arguments);

                if (function.Name == "SearchEvents")
                {
                    string keyword = args.TryGetProperty("keyword", out var k) ? k.GetString() : null;
                    string category = args.TryGetProperty("category", out var c) ? c.GetString() : null;
                    var result = await _eventService.BrowseEventsAsync(keyword, category, null, null, null, null, null, 1, 5);
                    return JsonSerializer.Serialize(result.Items, _jsonOptions);
                }
                else if (function.Name == "GetMyBookings")
                {
                    var bookings = await _bookingService.GetMyBookingsAsync(userId);
                    return JsonSerializer.Serialize(bookings, _jsonOptions);
                }
                else if (function.Name == "CancelBooking")
                {
                    if (args.TryGetProperty("bookingId", out var id))
                    {
                        bool success = await _bookingService.CancelBookingAsync(id.GetInt32());
                        return success ? "Booking cancelled successfully." : "Failed to cancel booking. It may have already been cancelled or is ineligible.";
                    }
                    return "Booking ID not provided.";
                }
                else if (function.Name == "GetMySupportTickets")
                {
                    var tickets = await _supportService.GetMySupportTicketsAsync(userId);
                    return JsonSerializer.Serialize(tickets, _jsonOptions);
                }
                else if (function.Name == "RaiseSupportTicket")
                {
                    if (args.TryGetProperty("subject", out var s) && args.TryGetProperty("message", out var m) && args.TryGetProperty("category", out var c))
                    {
                        int? relatedId = null;
                        if (args.TryGetProperty("relatedId", out var r))
                        {
                            if (r.ValueKind == JsonValueKind.Number) relatedId = r.GetInt32();
                            else if (r.ValueKind == JsonValueKind.String && int.TryParse(r.GetString(), out int id)) relatedId = id;
                        }
                        string? targetType = args.TryGetProperty("targetType", out var t) ? t.GetString() : null;
                        bool success = await _supportService.SubmitSupportTicketAsync(userId, s.GetString(), m.GetString(), c.GetString(), relatedId, targetType);
                        return success ? "Support ticket submitted successfully." : "Failed to submit support ticket.";
                    }
                    return "Subject, message, or category not provided.";
                }
                else if (function.Name == "GetPlatformNavigation")
                {
                    string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (rootPath.Contains("bin"))
                    {
                        rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    }
                    else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
                    {
                        rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    }
                    string navPath = Path.Combine(rootPath, "Event.Business", "assets", "agents", "navigations.json");
                    string navJson = await File.ReadAllTextAsync(navPath);
                    return navJson;
                }
                return $"Tool {function.Name} not found.";
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                await LogActivityAsync("ERROR", $"Error executing function {function.Name}: {ex.Message} - Inner: {msg}");
                return $"Error executing function {function.Name}: {ex.Message} - Inner: {msg}";
            }
            
            return $"Function {function.Name} not found or unsupported.";
        }

        #endregion
    }
}
