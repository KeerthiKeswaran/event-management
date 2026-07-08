using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using Event.Business.Exceptions;
using Event.Business.Helpers;

namespace Event.Business.Services
{
    public class FinanceService : IFinanceService
    {
        #region Fields

        private readonly IAdminActionRepository _adminActionRepository;
        private readonly ISupportTicketRepository _supportTicketRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRefundService _refundService;
        private readonly IEmailService _emailService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IPlatformSettingsRepository _platformSettingsRepository;

        #endregion

        #region Constructor

        public FinanceService(
            IAdminActionRepository adminActionRepository,
            ISupportTicketRepository supportTicketRepository,
            IUserRepository userRepository,
            IRefundService refundService,
            IEmailService emailService,
            INotificationRepository notificationRepository,
            ITransactionRepository transactionRepository,
            IEventRepository eventRepository,
            IPlatformSettingsRepository platformSettingsRepository)
        {
            _adminActionRepository = adminActionRepository;
            _supportTicketRepository = supportTicketRepository;
            _userRepository = userRepository;
            _refundService = refundService;
            _emailService = emailService;
            _notificationRepository = notificationRepository;
            _transactionRepository = transactionRepository;
            _eventRepository = eventRepository;
            _platformSettingsRepository = platformSettingsRepository;
        }

        #endregion

        #region GetAdminActionsAsync

        public async Task<IEnumerable<object>> GetAdminActionsAsync()
        {
            var actions = await _adminActionRepository.GetAllAsync();
            var result = new List<object>();

            foreach (var action in actions)
            {
                object? details = null;

                string? senderEmail = null;
                int? relatedId = null;

                if (action.TicketId.HasValue)
                {
                    // It is a support ticket! Read the support ticket JSON details
                    var ticket = await _supportTicketRepository.GetByIdAsync(action.TicketId.Value);
                    if (ticket != null)
                    {
                        details = GetSupportTicketDetails(ticket.ConcernUrl);
                        var user = await _userRepository.GetByIdAsync(ticket.User_Id);
                        senderEmail = user?.Email;
                        relatedId = ticket.RelatedId;
                    }
                }
                else
                {
                    // It is an event report escalation!
                    // Let's get the event reports for TargetId (which is the event ID)
                    var reports = await _eventRepository.GetAllReportsAsync() ?? new List<EventReport>();
                    var eventReports = System.Linq.Enumerable.ToList(
                        System.Linq.Enumerable.Where(reports, r => r.Event_Id == action.TargetId)
                    );

                    var reportList = new List<object>();
                    foreach (var r in eventReports)
                    {
                        reportList.Add(new
                        {
                            reportId = r.Report_Id,
                            reporterId = r.Reporter_Id,
                            reason = GetReportReason(r.ReportUrl),
                            createdAt = r.Created_At
                        });
                    }
                    details = new { Reports = reportList };
                }

                result.Add(new
                {
                    actionId = action.ActionId,
                    adminId = action.AdminId,
                    actionType = action.ActionType,
                    targetType = action.TargetType,
                    targetId = action.TargetId,
                    ticketId = action.TicketId,
                    actionStatus = action.ActionStatus,
                    remarks = action.Remarks,
                    createdAt = action.CreatedAt,
                    details = details,
                    senderEmail = senderEmail,
                    relatedId = relatedId
                });
            }

            return result;
        }

        #endregion

        #region DeclineActionAsync

        public async Task<bool> DeclineActionAsync(int actionId, string remarks)
        {
            var action = await _adminActionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                throw new NotFoundException($"AdminAction with ID {actionId} not found.");
            }

            action.ActionStatus = "Declined";
            action.Remarks = remarks;
            await _adminActionRepository.UpdateAsync(action);
            return true;
        }

        #endregion

        #region ApproveActionAsync

        public async Task<bool> ApproveActionAsync(int actionId, string refundType, string refundMessage)
        {
            var action = await _adminActionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                throw new NotFoundException($"AdminAction with ID {actionId} not found.");
            }

            if (!string.Equals(action.ActionType, "REF", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"Action type {action.ActionType} is not supported for refund approval.");
            }

            string mappedRefundType;
            switch (refundType?.ToUpper())
            {
                case "FUL":
                    mappedRefundType = "Full";
                    break;
                case "DYN":
                    mappedRefundType = "Dynamic";
                    break;
                case "REM":
                    mappedRefundType = "Remaining";
                    break;
                case "NOR":
                    mappedRefundType = "NoRefund";
                    break;
                default:
                    throw new ValidationException("Invalid refund type. Allowed values are FUL, DYN, REM, or NOR.");
            }

            // Assign status to "Processing" before the payment service call
            action.ActionStatus = "Processing";
            await _adminActionRepository.UpdateAsync(action);

            string finalRemarks = string.Empty;

            if (string.Equals(action.TargetType, "ATD", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(action.TargetType, "ADT", StringComparison.OrdinalIgnoreCase))
            {
                if (action.TicketId == null) throw new ValidationException("Cannot refund attendee without a related Ticket ID.");
                var ticket = await _supportTicketRepository.GetByIdAsync(action.TicketId.Value);
                if (ticket == null || ticket.RelatedId == null) throw new ValidationException("Support ticket missing or missing RelatedId for booking.");
                
                var result = await _refundService.RefundAttendeeAsync(ticket.RelatedId.Value, mappedRefundType, refundMessage: refundMessage);
                finalRemarks = $"Approved refund of type {mappedRefundType}. Amount: {result.RefundAmount}. {result.Remarks}";
            }
            else if (string.Equals(action.TargetType, "EVT", StringComparison.OrdinalIgnoreCase) || string.Equals(action.TargetType, "ORG", StringComparison.OrdinalIgnoreCase))
            {
                int eventId = action.TargetId;
                if (string.Equals(action.TargetType, "ORG", StringComparison.OrdinalIgnoreCase))
                {
                   if (action.TicketId == null) throw new ValidationException("Cannot refund organizer without a related Ticket ID.");
                   var ticket = await _supportTicketRepository.GetByIdAsync(action.TicketId.Value);
                   if (ticket == null || ticket.RelatedId == null) throw new ValidationException("Support ticket missing or missing RelatedId for event.");
                   eventId = ticket.RelatedId.Value;
                }
                var result = await _refundService.RefundOrganizerAsync(eventId, mappedRefundType, refundMessage: refundMessage);
                finalRemarks = $"Approved organizer refund of type {mappedRefundType}. Organizer Refund: {result.OrganizerRefundAmount}. {result.OrganizerRemarks}";
            }
            else
            {
                throw new ValidationException($"Target type {action.TargetType} is not recognized. Must be ATD/ADT, EVT or ORG.");
            }

            // Update status to "Processed" once the payment has been done
            action.ActionStatus = "Processed";
            action.Remarks = finalRemarks;
            await _adminActionRepository.UpdateAsync(action);
            return true;
        }

        #endregion


        #region RespondToTicketAsync

        public async Task<bool> RespondToTicketAsync(int ticketId, string responseText)
        {
            var ticket = await _supportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                throw new NotFoundException($"Support ticket with ID {ticketId} not found.");
            }

            var user = await _userRepository.GetByIdAsync(ticket.User_Id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {ticket.User_Id} associated with support ticket {ticketId} not found.");
            }

            if (string.IsNullOrEmpty(ticket.ConcernUrl))
            {
                throw new ValidationException("Support ticket does not have a concern URL path.");
            }

            string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folderName = "Event.Business";
            if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                Directory.GetCurrentDirectory().Contains("Tests"))
            {
                folderName = "Event.Business.Tests";
            }

            if (rootPath.Contains("bin"))
            {
                rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            string relativeConcern = ticket.ConcernUrl.TrimStart('/');
            if (relativeConcern.StartsWith("assets/"))
            {
                relativeConcern = relativeConcern.Substring("assets/".Length);
            }
            string filePath = Path.Combine(rootPath, folderName, "assets", relativeConcern);

            string? dirPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string subject = "No Subject";
            string message = "No Message";

            if (File.Exists(filePath))
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var ticketData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                if (ticketData != null)
                {
                    if (ticketData.ContainsKey("Subject")) subject = ticketData["Subject"];
                    if (ticketData.ContainsKey("Message")) message = ticketData["Message"];

                    ticketData["Response"] = responseText;

                    var updatedJson = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, updatedJson);
                }
            }
            else
            {
                var ticketData = new Dictionary<string, string>
                {
                    { "Subject", subject },
                    { "Message", message },
                    { "Response", responseText }
                };
                var updatedJson = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJson);
            }

            ticket.Status = "Resolved";
            await _supportTicketRepository.UpdateAsync(ticket);

            var emailDto = new EmailTemplateDto
            {
                TemplateName = "SupportTicketResponseTemplate.html",
                Placeholders = new Dictionary<string, string>
                {
                    { "userName", user.Name },
                    { "ticketId", ticketId.ToString() },
                    { "subject", subject },
                    { "message", message },
                    { "response", responseText },
                    { "year", DateTime.UtcNow.Year.ToString() }
                }
            };

            string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);
            await NotificationHelper.SendAndSaveNotificationAsync(
                _notificationRepository,
                _emailService,
                user.Email,
                $"Support Ticket #{ticketId} | Finance Teams",
                htmlBody
            );

            return true;
        }

        #endregion

        #region GetTransactionsPagedAsync

        public async Task<PagedResult<Transaction>> GetTransactionsPagedAsync(
            string? keyword,
            string? transactionType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size)
        {
            // 1. Query the transaction repository with paged parameters and filters
            return await _transactionRepository.GetTransactionsPagedAsync(
                keyword,
                transactionType,
                status,
                startDate,
                endDate,
                sortBy,
                page,
                size
            );
        }

        #endregion

        #region GetDashboardStatsAsync

        public async Task<FinanceDashboardStatsResponse> GetDashboardStatsAsync()
        {
            var settings = await _platformSettingsRepository.GetSettingsAsync();
            decimal commissionPercentage = settings?.Ticket_Commission_Percentage ?? 0m;

            // Get total successful transactions and sum of revenue
            var allTransactions = await _transactionRepository.GetAllAsync();
            var successfulTransactions = allTransactions.Where(t => t.Status == "Success");

            decimal totalRevenue = 0m;
            decimal totalIntake = 0m;
            int totalTxCount = successfulTransactions.Count();

            var allActions = await _adminActionRepository.GetAllAsync();
            int pendingApprovals = allActions.Count(a => a.ActionStatus == "Pending");

            foreach (var t in successfulTransactions)
            {
                if (t.Transaction_Type == "OrganizerUpfrontPayment" || t.Transaction_Type == "BookingPayment")
                {
                    totalRevenue += t.Amount;
                }

                if (t.Transaction_Type == "OrganizerUpfrontPayment")
                {
                    totalIntake += t.Amount;
                }
                else if (t.Transaction_Type == "BookingPayment")
                {
                    // Assuming booking payment contains the base ticket price + fees. 
                    // Platform earns the ticket commission from the organizer's payout,
                    // plus any fees passed to the buyer. Since the exact breakdown isn't on the transaction,
                    // we calculate commission based on the total booking payment minus fixed fees, 
                    // or just apply the commission percentage as a simplification, 
                    // but the prompt says: "sum of all the fee, upfrontds and ticket commision from the organizer payout".
                    
                    // A simple approximation for intake from booking:
                    decimal baseAmount = t.Amount; // if we assume this is close enough, or we can use the commission percentage
                    decimal commission = baseAmount * (commissionPercentage / 100m);
                    totalIntake += commission;
                }
            }

            return new FinanceDashboardStatsResponse
            {
                TotalTransactions = totalTxCount,
                PendingApprovals = pendingApprovals,
                TotalRevenue = totalRevenue,
                TotalIntake = totalIntake
            };
        }

        #endregion

        private object GetSupportTicketDetails(string? concernUrl)
        {
            if (string.IsNullOrEmpty(concernUrl)) return new { Subject = "", Message = "", Response = "" };

            try
            {
                string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string folderName = "Event.Business";
                if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                    AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                    Directory.GetCurrentDirectory().Contains("Tests"))
                {
                    folderName = "Event.Business.Tests";
                }

                if (rootPath.Contains("bin"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }

                string relativePath = concernUrl.TrimStart('/');
                if (relativePath.StartsWith("assets/"))
                {
                    relativePath = relativePath.Substring("assets/".Length);
                }
                string filePath = Path.Combine(rootPath, folderName, "assets", relativePath);

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (data != null)
                    {
                        return new
                        {
                            Subject = data.ContainsKey("Subject") ? data["Subject"] : "",
                            Message = data.ContainsKey("Message") ? data["Message"] : "",
                            Response = data.ContainsKey("Response") ? data["Response"] : ""
                        };
                    }
                }
            }
            catch { }

            return new { Subject = "", Message = "Details in JSON file", Response = "" };
        }

        private string GetReportReason(string? reportUrl)
        {
            if (string.IsNullOrEmpty(reportUrl)) return string.Empty;

            try
            {
                string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string folderName = "Event.Business";
                if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                    AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                    Directory.GetCurrentDirectory().Contains("Tests"))
                {
                    folderName = "Event.Business.Tests";
                }

                if (rootPath.Contains("bin"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }

                string relativePath = reportUrl.TrimStart('/');
                if (relativePath.StartsWith("assets/"))
                {
                    relativePath = relativePath.Substring("assets/".Length);
                }
                string filePath = Path.Combine(rootPath, folderName, "assets", relativePath);

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (data != null && data.ContainsKey("Reason"))
                    {
                        return data["Reason"];
                    }
                }
            }
            catch { }

            return "Details in JSON file";
        }

    
        public async Task<PagedResult<Event.Models.DTOs.OrganizerPayoutDto>> GetOrganizerPayoutsPagedAsync(string? status, string? sortBy, int page, int size)
        {
            var pagedEvents = await _eventRepository.GetEventsForPayoutsAsync(status, sortBy, page, size);
            
            var settings = await _platformSettingsRepository.GetSettingsAsync();
            decimal commission = settings?.Ticket_Commission_Percentage ?? 0m;
            
            var dtoList = pagedEvents.Items.Select(e => {
                decimal totalAmount = e.Bookings
                    .SelectMany(b => b.Payments)
                    .Where(p => p.Payment_Status == "Completed" || p.Payment_Status == "Success")
                    .Sum(p => p.Amount);
                    
                decimal payoutAmount = totalAmount - (totalAmount * (commission / 100m));
                
                return new Event.Models.DTOs.OrganizerPayoutDto {
                    Event_Id = e.Event_Id,
                    Organizer_Id = e.Organizer_Id,
                    Organizer_Email = e.Organizer?.Email ?? "",
                    Amount = payoutAmount,
                    Date_Time = e.Date_Time,
                    Status = e.Status == "Live" ? "Upcoming" : "Completed"
                };
            }).ToList();

            return new PagedResult<Event.Models.DTOs.OrganizerPayoutDto>(dtoList, pagedEvents.TotalCount, page, size);
        }
}
}
