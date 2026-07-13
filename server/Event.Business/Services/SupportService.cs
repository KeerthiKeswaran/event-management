using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Event.Contracts.IRepositories;
using Event.Models;
using Event.Business.Exceptions;

namespace Event.Business.Services
{
    public class SupportService : ISupportService
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly ISupportTicketRepository _supportTicketRepository;
        private readonly IFileStorageService _fileStorageService;

        #endregion

        #region Constructor

        public SupportService(IUserRepository userRepository, ISupportTicketRepository supportTicketRepository, IFileStorageService fileStorageService)
        {
            _userRepository = userRepository;
            _supportTicketRepository = supportTicketRepository;
            _fileStorageService = fileStorageService;
        }

        #endregion

        #region SubmitSupportTicketAsync

        public async Task<bool> SubmitSupportTicketAsync(int userId, string subject, string message, string requestType, int? relatedId = null, string? targetType = null)
        {
            // 1. Validate that target user exists
            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
                throw new NotFoundException($"User with ID {userId} not found.");

            string? escalationStatus = null;
            if (string.Equals(requestType, "REF", System.StringComparison.OrdinalIgnoreCase))
            {
                escalationStatus = "Available";
            }
            else
            {
                escalationStatus = "Unavailable";
            }

            string fileName = $"ticket_{System.DateTime.UtcNow.Ticks}.json";
            string relativePath = $"users/{userId}/support/{fileName}";

            var ticketData = new
            {
                Subject = subject,
                Message = message,
                Response = (string?)null
            };

            string jsonContent = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
            string url = await _fileStorageService.SaveTextAsync(relativePath, jsonContent);

            // 3. Instantiate and persist new support ticket with "Open" status
            var ticket = new SupportTicket
            {
                User_Id = userId,
                ConcernUrl = url,
                RequestType = requestType,
                Status = "Open",
                EsclationStatus = escalationStatus,
                RelatedId = relatedId,
                TargetType = targetType
            };
            await _supportTicketRepository.AddAsync(ticket);

            return true;
        }

        #endregion

        #region GetMySupportTicketsAsync

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.DTOs.SupportTicketDto>> GetMySupportTicketsAsync(int userId)
        {
            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
                throw new NotFoundException($"User with ID {userId} not found.");

            var tickets = await _supportTicketRepository.GetTicketsByUserIdAsync(userId);

            var result = new System.Collections.Generic.List<Event.Models.DTOs.SupportTicketDto>();

            foreach (var t in tickets)
            {
                string subject = "No Subject";
                string message = "No Message";
                string? response = null;

                if (!string.IsNullOrEmpty(t.ConcernUrl))
                {
                    string relativeConcern = t.ConcernUrl;
                    if (relativeConcern.StartsWith("/assets/"))
                    {
                        relativeConcern = relativeConcern.Substring("/assets/".Length);
                    }

                    string jsonContent = await _fileStorageService.ReadTextAsync(relativeConcern);
                    
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        try
                        {
                            var ticketData = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(jsonContent);
                            if (ticketData != null)
                            {
                                if (ticketData.ContainsKey("Subject") && ticketData["Subject"] != null) subject = ticketData["Subject"];
                                if (ticketData.ContainsKey("Message") && ticketData["Message"] != null) message = ticketData["Message"];
                                if (ticketData.ContainsKey("Response") && ticketData["Response"] != null) response = ticketData["Response"];
                            }
                        }
                        catch
                        {
                            // ignore json parse errors
                        }
                    }
                }

                result.Add(new Event.Models.DTOs.SupportTicketDto
                {
                    TicketId = $"TKT-{t.Ticket_Id}",
                    BookingId = t.RelatedId?.ToString() ?? "General",
                    Category = t.RequestType,
                    Subject = subject,
                    Details = message,
                    Status = t.Status,
                    Response = response,
                    CreatedAt = t.CreatedAt?.ToString("O") ?? System.DateTime.UtcNow.ToString("O")
                });
            }

            // sort by ID descending (newest first)
            result.Reverse();
            return result;
        }

        #endregion
    }
}
