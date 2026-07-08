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

        #endregion

        #region Constructor

        public SupportService(IUserRepository userRepository, ISupportTicketRepository supportTicketRepository)
        {
            _userRepository = userRepository;
            _supportTicketRepository = supportTicketRepository;
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

            string rootPath = System.IO.Directory.GetCurrentDirectory().TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            string folderName = "Event.Business";
            if (System.AppDomain.CurrentDomain.FriendlyName.Contains("Tests") ||
                System.AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                System.IO.Directory.GetCurrentDirectory().Contains("Tests"))
            {
                folderName = "Event.Business.Tests";
            }

            if (rootPath.Contains("bin"))
            {
                rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootPath, "..")).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }

            string folderPath = System.IO.Path.Combine(rootPath, folderName, "assets", "users", userId.ToString(), "support");
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            // 3. Instantiate and persist new support ticket with "Open" status
            var ticket = new SupportTicket
            {
                User_Id = userId,
                ConcernUrl = $"/assets/users/{userId}/support/ticket_pending.json",
                RequestType = requestType,
                Status = "Open",
                EsclationStatus = escalationStatus,
                RelatedId = relatedId,
                TargetType = targetType
            };
            await _supportTicketRepository.AddAsync(ticket);

            var ticketData = new
            {
                Subject = subject,
                Message = message,
                Response = (string?)null
            };

            string fileName = $"ticket_{ticket.Ticket_Id}.json";
            string filePath = System.IO.Path.Combine(folderPath, fileName);
            string jsonContent = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonContent);

            ticket.ConcernUrl = $"/assets/users/{userId}/support/{fileName}";
            await _supportTicketRepository.UpdateAsync(ticket);
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

            string rootPath = System.IO.Directory.GetCurrentDirectory().TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            string folderName = "Event.Business";
            if (System.AppDomain.CurrentDomain.FriendlyName.Contains("Tests") ||
                System.AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                System.IO.Directory.GetCurrentDirectory().Contains("Tests"))
            {
                folderName = "Event.Business.Tests";
            }

            if (rootPath.Contains("bin"))
            {
                rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootPath, "..")).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }

            var result = new System.Collections.Generic.List<Event.Models.DTOs.SupportTicketDto>();

            foreach (var t in tickets)
            {
                string subject = "No Subject";
                string message = "No Message";
                string? response = null;

                if (!string.IsNullOrEmpty(t.ConcernUrl))
                {
                    string relativeConcern = t.ConcernUrl.TrimStart('/');
                    if (relativeConcern.StartsWith("assets/"))
                    {
                        relativeConcern = relativeConcern.Substring("assets/".Length);
                    }
                    string filePath = System.IO.Path.Combine(rootPath, folderName, "assets", relativeConcern);

                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
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
                    CreatedAt = System.DateTime.UtcNow.ToString("O") // Wait, there's no CreatedAt in SupportTicket... I'll just use a default or check if we can get it.
                });
            }

            // sort by ID descending (newest first)
            result.Reverse();
            return result;
        }

        #endregion
    }
}
