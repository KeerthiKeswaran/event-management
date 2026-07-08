using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class AdminService : IAdminService
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IBookingPaymentRepository _bookingPaymentRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly ISupportTicketRepository _supportTicketRepository;
        private readonly IAdminActionRepository _adminActionRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IEmailService _emailService;
        private readonly IEventService _eventService;
        private readonly IRegionRepository _regionRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly INotificationRepository _notificationRepository;

        #endregion

        #region Constructor

        public AdminService(
            IUserRepository userRepository,
            IEventRepository eventRepository,
            ITransactionRepository transactionRepository,
            IBookingRepository bookingRepository,
            IBookingPaymentRepository bookingPaymentRepository,
            IStaffRepository staffRepository,
            ISupportTicketRepository supportTicketRepository,
            IAdminActionRepository adminActionRepository,
            IAdminRepository adminRepository,
            IEmailService emailService,
            IEventService eventService,
            IRegionRepository regionRepository,
            IVenueRepository venueRepository,
            INotificationRepository notificationRepository)
        {
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _transactionRepository = transactionRepository;
            _bookingRepository = bookingRepository;
            _bookingPaymentRepository = bookingPaymentRepository;
            _staffRepository = staffRepository;
            _supportTicketRepository = supportTicketRepository;
            _adminActionRepository = adminActionRepository;
            _adminRepository = adminRepository;
            _emailService = emailService;
            _eventService = eventService;
            _regionRepository = regionRepository;
            _venueRepository = venueRepository;
            _notificationRepository = notificationRepository;
        }

        #endregion


        #region GetDashboardStatsAsync

        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
        {
            // 1. Gather summary numbers
            var users = await _userRepository.GetAllAsync();
            int totalUsers = users.Count();

            var events = await _eventRepository.GetAllAsync();
            int totalLiveEvents = events.Count(e => e.Status == "Live");

            decimal grossRevenue = await _transactionRepository.GetGrossRevenueAsync();
            decimal platformCommission = await _bookingPaymentRepository.GetTotalCommissionAsync();

            // 2. Gather staff metrics
            var staffList = await _staffRepository.GetAllAsync();
            int totalStaff = staffList.Count();
            int allocatedStaffCount = staffList.Count(s => s.IsAllocated);
            double allocationPercentage = totalStaff > 0 ? ((double)allocatedStaffCount / totalStaff) * 100.0 : 0.0;

            return new AdminDashboardStatsDto
            {
                Summary = new StatsSummaryDto
                {
                    TotalUsers = totalUsers,
                    TotalLiveEvents = totalLiveEvents,
                    GrossRevenue = grossRevenue,
                    PlatformCommission = platformCommission
                },
                StaffMetrics = new StaffMetricsDto
                {
                    TotalStaff = totalStaff,
                    AllocatedStaffCount = allocatedStaffCount,
                    AllocationPercentage = Math.Round(allocationPercentage, 2)
                }
            };
        }

        #endregion

        #region GetEventsPagedAsync

        public async Task<PagedResult<EventDetailEto>> GetEventsPagedAsync(
            string? keyword,
            string? eventType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size)
        {
            var pagedEvents = await _eventRepository.GetEventsPagedAsync(keyword, eventType, status, startDate, endDate, sortBy, page, size);

            var mappedItems = pagedEvents.Items.Select(e =>
            {
                int totalSeats = e.Venue?.SeatCapacities?.Sum(c => c.Total_Seats) ?? 0;
                int requiredStaff = e.Requires_Staff && e.Venue != null ? Math.Max(1, (int)Math.Ceiling(totalSeats / 100.0)) : 0;
                double allocatedStaffPercentage = 100.0;
                if (e.Requires_Staff && requiredStaff > 0)
                {
                    int allocatedStaffCount = e.StaffAllocations?.Count ?? 0;
                    allocatedStaffPercentage = ((double)allocatedStaffCount / requiredStaff) * 100.0;
                }

                return new EventDetailEto
                {
                    EventId = e.Event_Id,
                    Title = e.Title,
                    EventType = e.Event_Type,
                    DateTime = e.Date_Time,
                    VenueName = e.Venue?.Name ?? "N/A (Virtual)",
                    OrganizerName = e.Organizer?.Name ?? "N/A",
                    OrganizerEmail = e.Organizer?.Email ?? "N/A",
                    AllocatedStaffCount = e.StaffAllocations?.Count ?? 0,
                    Status = e.Status,
                    AllocatedStaffPercentage = Math.Round(allocatedStaffPercentage, 2),
                    DescriptionUrl = e.Description_Url,
                    ImageUrl = e.Image_Url
                };
            }).ToList();

            return new PagedResult<EventDetailEto>(mappedItems, pagedEvents.TotalCount, pagedEvents.Page, pagedEvents.PageSize);
        }

        #endregion

        #region GetRelatedEntityAsync
        public async Task<object?> GetRelatedEntityAsync(string type, int id)
        {
            // The user explicitly requested to only return Booking or Event details, 
            // as the ID could belong to either regardless of the 'type' parameter.
            
            var booking = await _bookingRepository.GetBookingDetailsAsync(id);
            if (booking != null)
            {
                var successTx = await _transactionRepository.GetSuccessBookingTransactionAsync(id);
                return new {
                    Type = "Booking",
                    Id = booking.Booking_Id,
                    EventName = booking.Event?.Title ?? "Unknown Event",
                    TicketTiers = booking.Details?.Select(d => new { Tier = d.Tier_Name, Quantity = d.Quantity }),
                    AmountPaid = successTx?.Amount ?? 0,
                    AmountRefunded = successTx?.Refunded_Amount ?? 0
                };
            }

            var ev = await _eventRepository.GetEventDetailsAsync(id);
            if (ev != null)
            {
                var upfrontTx = await _transactionRepository.GetSuccessOrganizerUpfrontTransactionAsync(id);
                return new {
                    Type = "Event",
                    Id = ev.Event_Id,
                    EventName = ev.Title,
                    UpfrontPaid = upfrontTx?.Amount ?? 0,
                    UpfrontRefunded = upfrontTx?.Refunded_Amount ?? 0
                };
            }

            return null;
        }
        #endregion

        #region GetSupportTicketsAsync

        public async Task<EventDetailEto?> GetEventByIdAsync(int id)
        {
            var e = await _eventRepository.GetEventDetailsAsync(id);
            if (e == null) return null;

            return new EventDetailEto
            {
                EventId = e.Event_Id,
                Title = e.Title,
                EventType = e.Event_Type,
                Status = e.Status,
                DateTime = e.Date_Time,
                VenueName = e.Venue?.Name ?? string.Empty,
                OrganizerName = e.Organizer?.Name ?? e.Organizer?.Email ?? string.Empty,
                OrganizerEmail = e.Organizer?.Email ?? string.Empty,
                AllocatedStaffCount = e.StaffAllocations?.Count ?? 0,
                ImageUrl = e.Image_Url,
                DescriptionUrl = e.Description_Url
            };
        }

        public async Task<IEnumerable<SupportTicketResponse>> GetSupportTicketsAsync(string? status, string? keyword, DateTime? dateFrom, DateTime? dateTo)
        {
            var tickets = await _supportTicketRepository.GetAllWithUsersAsync();

            if (!string.IsNullOrWhiteSpace(status))
                tickets = tickets.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(keyword))
                tickets = tickets.Where(t =>
                    t.Ticket_Id.ToString().Contains(keyword) ||
                    (t.User?.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (t.User?.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (t.RequestType?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));

            var response = new List<SupportTicketResponse>();
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            string folderName = "Event.Business";
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(rootPath, folderName)))
            {
                rootPath = System.IO.Directory.GetCurrentDirectory();
            }

            var actions = await _adminActionRepository.GetAllAsync();

            foreach (var t in tickets)
            {
                var currentEscStatus = t.EsclationStatus;
                if (currentEscStatus == "Escalated")
                {
                    var relatedAction = actions.FirstOrDefault(a => a.TicketId == t.Ticket_Id);
                    if (relatedAction != null && (relatedAction.ActionStatus == "Processed" || relatedAction.ActionStatus == "Declined"))
                    {
                        currentEscStatus = relatedAction.ActionStatus;
                    }
                }

                var dto = new SupportTicketResponse
                {
                    Ticket_Id = t.Ticket_Id,
                    User_Id = t.User_Id,
                    SenderName = t.User?.Name ?? "Unknown",
                    SenderEmail = t.User?.Email ?? "Unknown",
                    RequestType = t.RequestType,
                    ConcernUrl = t.ConcernUrl,
                    Status = t.Status,
                    EsclationStatus = currentEscStatus,
                    RelatedId = t.RelatedId,
                    TargetType = t.TargetType,
                    Created_At = t.CreatedAt
                };

                if (!string.IsNullOrEmpty(t.ConcernUrl))
                {
                    string relativeConcern = t.ConcernUrl.TrimStart('/');
                    if (relativeConcern.StartsWith("assets/"))
                        relativeConcern = relativeConcern.Substring("assets/".Length);

                    string filePath = System.IO.Path.Combine(rootPath, folderName, "assets", relativeConcern);
                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            string json = await System.IO.File.ReadAllTextAsync(filePath);
                            var content = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                            if (content.TryGetProperty("Subject", out var subjectProp))
                            {
                                dto.Subject = subjectProp.GetString();
                            }
                            if (content.TryGetProperty("Message", out var messageProp))
                            {
                                dto.Message = messageProp.GetString();
                            }
                        }
                        catch { }
                    }
                }

                response.Add(dto);
            }

            return response;
        }

        #endregion

        #region RespondToTicketAsync

        public async Task<bool> RespondToTicketAsync(int ticketId, string responseText)
        {
            // 1. Fetch support ticket and validate existence
            var ticket = await _supportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                throw new NotFoundException($"Support ticket with ID {ticketId} not found.");
            }

            // 2. Fetch associated user to get name and email
            var user = await _userRepository.GetByIdAsync(ticket.User_Id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {ticket.User_Id} associated with support ticket {ticketId} not found.");
            }

            if (string.IsNullOrEmpty(ticket.ConcernUrl))
            {
                throw new ValidationException("Support ticket does not have a concern URL path.");
            }

            // 3. Edit the local JSON file containing Subject, Message, and Response
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
                // Fallback in case JSON file doesn't exist, create it
                var ticketData = new Dictionary<string, string>
                {
                    { "Subject", subject },
                    { "Message", message },
                    { "Response", responseText }
                };
                var updatedJson = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJson);
            }

            // 4. Update the ticket status in database to Resolved
            ticket.Status = "Resolved";
            await _supportTicketRepository.UpdateAsync(ticket);

            // 5. Build and send the response email
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
                $"Support Ticket #{ticketId} Responded",
                htmlBody
            );

            return true;
        }

        #endregion

        #region EscalateTicketAsync

        public async Task<bool> EscalateTicketAsync(int ticketId, string adminId, EscalateTicketRequest request)
        {
            // 1. Fetch support ticket and validate existence
            var ticket = await _supportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                throw new NotFoundException($"Support ticket with ID {ticketId} not found.");
            }

            // 2. Add an AdminAction record with ActionStatus as "Pending" using values from request DTO
            var action = new AdminAction
            {
                AdminId = adminId,
                ActionType = request.ActionType,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                TicketId = ticketId,
                ActionStatus = "Pending",
                Remarks = $"Ticket #{ticketId} escalated.",
                CreatedAt = DateTime.UtcNow
            };
            await _adminActionRepository.AddAsync(action);

            // 3. Mark the escalation status as Escalated
            ticket.EsclationStatus = "Escalated";
            await _supportTicketRepository.UpdateAsync(ticket);

            return true;
        }

        #endregion

        #region GetEscalationStatusAsync

        public async Task<AdminAction?> GetEscalationStatusAsync(int ticketId)
        {
            var actions = await _adminActionRepository.GetAllAsync();
            return actions.Where(a => a.TicketId == ticketId)
                          .OrderByDescending(a => a.CreatedAt)
                          .FirstOrDefault();
        }

        #endregion

        #region GetFlaggedEventsReportsAsync

        public async Task<object> GetFlaggedEventsReportsAsync()
        {
            var reports = await _eventRepository.GetAllReportsAsync();
            
            var grouped = new System.Collections.Generic.Dictionary<int, object>();
            var reportsByEvent = System.Linq.Enumerable.GroupBy(reports, r => r.Event_Id);
            
            foreach (var group in reportsByEvent)
            {
                int eventId = group.Key;
                var list = new System.Collections.Generic.List<object>();
                
                foreach (var r in group)
                {
                    list.Add(new
                    {
                        reportId = r.Report_Id,
                        reporterId = r.Reporter_Id,
                        reporterName = r.Reporter?.Name ?? "Unknown",
                        reason = GetReasonFromReportUrl(r.ReportUrl),
                        responseAction = r.ResponseAction,
                        createdAt = r.Created_At
                    });
                }
                
                grouped[eventId] = new
                {
                    countOfReports = group.Count(),
                    reports = list
                };
            }
            
            return grouped;
        }

        private static string GetReasonFromReportUrl(string reportUrl)
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
                    var data = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(jsonContent);
                    if (data != null && data.ContainsKey("Reason"))
                    {
                        return data["Reason"];
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return "Details in JSON file";
        }

        #endregion

        #region DismissEventReportAsync

        public async Task<bool> DismissEventReportAsync(int reportId)
        {
            var report = await _eventRepository.GetReportByIdAsync(reportId);
            if (report == null)
                throw new NotFoundException($"EventReport with ID {reportId} not found.");

            report.ResponseAction = "Dismissed";
            await _eventRepository.UpdateReportAsync(report);
            return true;
        }

        #endregion

        #region UpholdEventReportAsync

        public async Task<bool> UpholdEventReportAsync(int reportId, string adminId, string actionReason, string organizerAction)
        {
            // 1. Fetch report details
            var report = await _eventRepository.GetReportByIdAsync(reportId);
            if (report == null)
                throw new NotFoundException($"EventReport with ID {reportId} not found.");

            var ev = report.Event;
            if (ev == null)
                throw new NotFoundException($"Event associated with report {reportId} not found.");

            // 2. Cancel the event and trigger email to the organizer
            if (ev.Status != "Cancelled")
            {
                ev.Status = "Cancelled";
                await _eventRepository.UpdateAsync(ev);
            }

            var organizer = await _userRepository.GetByIdAsync(ev.Organizer_Id);

            try
            {
                var eventCancelEmailDto = new EmailTemplateDto
                {
                    TemplateName = "EventCancellationTemplate.html",
                    Placeholders = new Dictionary<string, string>
                    {
                        { "eventName", ev.Title },
                        { "refundStatusMessage", "A refund will be processed shortly if applicable." },
                        { "year", DateTime.UtcNow.Year.ToString() }
                    }
                };

                string htmlCancelBody = await _emailService.BuildEmailHtmlAsync(eventCancelEmailDto);
                await NotificationHelper.SendAndSaveNotificationAsync(
                    _notificationRepository,
                    _emailService,
                    organizer?.Email ?? ev.Organizer?.Email ?? string.Empty,
                    $"Event Cancelled: {ev.Title}",
                    htmlCancelBody
                );
            }
            catch (Exception) { }

            // 3. Update organizer status based on organizerAction status
            if (organizer != null)
            {
                if (string.Equals(organizerAction, "Restrict", StringComparison.OrdinalIgnoreCase))
                {
                    organizer.Status = "Restricted";
                    await _userRepository.UpdateAsync(organizer);
                }
                else if (string.Equals(organizerAction, "Deactivate", StringComparison.OrdinalIgnoreCase))
                {
                    organizer.Status = "Deactivated";
                    await _userRepository.UpdateAsync(organizer);
                }
                // If "No Action", do not change status.
            }

            // 4. Save AdminAction in database
            var action = new AdminAction
            {
                AdminId = adminId,
                ActionType = "REF",
                TargetType = "EVT",
                TargetId = ev.Event_Id,
                TicketId = null,
                ActionStatus = "Pending",
                Remarks = $"Event #{ev.Event_Id} flagged and report upheld. Escalated for refund.",
                CreatedAt = DateTime.UtcNow
            };
            await _adminActionRepository.AddAsync(action);

            // 5. Update report state to Upholds
            report.ResponseAction = "Upholds";
            await _eventRepository.UpdateReportAsync(report);

            return true;
        }

        #endregion

        #region GetAllRegionsAsync

        public async Task<IEnumerable<RegionResponse>> GetAllRegionsAsync()
        {
            var regions = await _regionRepository.GetAllAsync();
            return regions.Select(r => new RegionResponse
            {
                Region_Id = r.Region_Id,
                No_Of_Staffs = r.No_Of_Staffs,
                Region_Name = r.Region_Name
            }).ToList();
        }

        #endregion

        #region GetAllVenuesAsync

        public async Task<IEnumerable<VenueResponse>> GetAllVenuesAsync()
        {
            var venues = await _venueRepository.GetAllWithDetailsAsync();

            return venues
                .Where(v => v.Is_Available)
                .Select(v => new VenueResponse
            {
                Venue_Id     = v.Venue_Id,
                Region_Id    = v.Region_Id,
                Name         = v.Name,
                Address      = v.Address,
                Hourly_Price = v.Hourly_Price,
                Is_Available = v.Is_Available,
                SeatTiers    = v.SeatCapacities.Select(sc => new SeatTierResponse
                {
                    Tier_Name   = sc.Tier_Name,
                    Total_Seats = sc.Total_Seats
                }).ToList()
            }).ToList();
        }

        #endregion

        #region CreateVenueAsync

        public async Task<VenueResponse> CreateVenueAsync(CreateVenueRequest request)
        {
            if (request.SeatTiers == null || !request.SeatTiers.Any())
                throw new ValidationException("At least one seat tier is required.");

            // 2. Ensure the Region exists; create it if not
            var region = await _regionRepository.GetByRegionIdAsync(request.Region_Id);
            if (region == null)
            {
                region = new Region
                {
                    Region_Id    = request.Region_Id,
                    No_Of_Staffs = 0
                };
                await _regionRepository.AddAsync(region);
            }

            // 3. Build and persist the Venue
            var venue = new Venue
            {
                Region_Id    = request.Region_Id,
                Name         = request.Name,
                Address      = request.Address,
                Hourly_Price = request.Hourly_Price,
                Is_Available = request.Is_Available
            };
            await _venueRepository.AddAsync(venue);

            // 4. Persist each seat tier capacity linked to the new Venue_Id
            foreach (var tierReq in request.SeatTiers)
            {
                var seatCapacity = new VenueSeatCapacity
                {
                    Venue_Id    = venue.Venue_Id,
                    Tier_Name   = tierReq.Tier_Name,
                    Total_Seats = tierReq.Total_Seats
                };
                await _venueRepository.AddSeatCapacityAsync(seatCapacity);
            }

            // 5. Re-fetch the venue with navigation properties for the response
            var created = (await _venueRepository.GetAllWithDetailsAsync())
                .First(v => v.Venue_Id == venue.Venue_Id);

            return new VenueResponse
            {
                Venue_Id     = created.Venue_Id,
                Region_Id    = created.Region_Id,
                Name         = created.Name,
                Address      = created.Address,
                Hourly_Price = created.Hourly_Price,
                Is_Available = created.Is_Available,
                SeatTiers    = created.SeatCapacities.Select(sc => new SeatTierResponse
                {
                    Tier_Name   = sc.Tier_Name,
                    Total_Seats = sc.Total_Seats
                }).ToList()
            };
        }

        #endregion

        #region GetStaffDirectoryAsync
        
        public async Task<PagedResult<StaffResponse>> GetStaffDirectoryAsync(string? regionId, bool? isAllocated, string? keyword, string? sortBy, int page = 1, int size = 10)
        {
            var staffs = await _staffRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(regionId))
                staffs = staffs.Where(s => string.Equals(s.Region_Id, regionId, StringComparison.OrdinalIgnoreCase));

            if (isAllocated.HasValue)
                staffs = staffs.Where(s => s.IsAllocated == isAllocated.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                staffs = staffs.Where(s => 
                    s.Employee_ID.ToString().Contains(lowerKeyword) ||
                    (s.Name?.ToLower().Contains(lowerKeyword) ?? false) ||
                    (s.Email?.ToLower().Contains(lowerKeyword) ?? false)
                );
            }

            // Backend-driven sorting
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var isDesc = sortBy.EndsWith("_desc", StringComparison.OrdinalIgnoreCase);
                var sortCol = sortBy.Replace("_desc", "", StringComparison.OrdinalIgnoreCase).Replace("_asc", "", StringComparison.OrdinalIgnoreCase).ToLower();

                staffs = sortCol switch
                {
                    "employee_id" => isDesc ? staffs.OrderByDescending(s => s.Employee_ID) : staffs.OrderBy(s => s.Employee_ID),
                    "name" => isDesc ? staffs.OrderByDescending(s => s.Name) : staffs.OrderBy(s => s.Name),
                    "email" => isDesc ? staffs.OrderByDescending(s => s.Email) : staffs.OrderBy(s => s.Email),
                    "regionid" => isDesc ? staffs.OrderByDescending(s => s.Region?.Region_Name) : staffs.OrderBy(s => s.Region?.Region_Name),
                    "isallocated" => isDesc ? staffs.OrderByDescending(s => s.IsAllocated) : staffs.OrderBy(s => s.IsAllocated),
                    _ => staffs.OrderBy(s => s.Employee_ID)
                };
            }
            else
            {
                staffs = staffs.OrderBy(s => s.Employee_ID);
            }

            int totalCount = staffs.Count();
            int totalPages = (int)Math.Ceiling(totalCount / (double)size);

            var pagedStaffs = staffs
                .Skip((page - 1) * size)
                .Take(size)
                .Select(s => new StaffResponse
                {
                    Employee_ID = s.Employee_ID,
                    Name = s.Name,
                    Email = s.Email,
                    Region_Id = s.Region_Id,
                    Region_Name = s.Region?.Region_Name ?? s.Region_Id,
                    IsAllocated = s.IsAllocated
                }).ToList();

            return new PagedResult<StaffResponse>
            {
                Items = pagedStaffs,
                TotalCount = totalCount,
                Page = page,
                PageSize = size
            };
        }

        #endregion

        #region GetStaffByRegionAsync

        public async Task<IEnumerable<StaffResponse>> GetStaffByRegionAsync(string regionId)
        {
            var staffs = await _staffRepository.GetAllAsync();
            return staffs
                .Where(s => string.Equals(s.Region_Id, regionId, StringComparison.OrdinalIgnoreCase) && !s.IsAllocated)
                .Select(s => new StaffResponse
                {
                    Employee_ID = s.Employee_ID,
                    Name = s.Name,
                    Email = s.Email,
                    Region_Id = s.Region_Id,
                    IsAllocated = s.IsAllocated
                });
        }

        #endregion

        #region GetEventsByRegionAsync

        public async Task<IEnumerable<EventDetailEto>> GetEventsByRegionAsync(string regionId)
        {
            var events = await _eventRepository.GetEventsByRegionsAsync(new[] { regionId });

            return events.Select(e =>
            {
                int totalSeats = e.Venue?.SeatCapacities?.Sum(c => c.Total_Seats) ?? 0;
                int requiredStaff = e.Requires_Staff && e.Venue != null ? Math.Max(1, (int)Math.Ceiling(totalSeats / 100.0)) : 0;
                double allocatedStaffPercentage = 100.0;
                if (e.Requires_Staff && requiredStaff > 0)
                {
                    int allocatedStaffCount = e.StaffAllocations?.Count ?? 0;
                    allocatedStaffPercentage = ((double)allocatedStaffCount / requiredStaff) * 100.0;
                }

                return new EventDetailEto
                {
                    EventId = e.Event_Id,
                    Title = e.Title,
                    EventType = e.Event_Type,
                    DateTime = e.Date_Time,
                    VenueName = e.Venue?.Name ?? "N/A",
                    OrganizerName = e.Organizer?.Name ?? "N/A",
                    AllocatedStaffCount = e.StaffAllocations?.Count ?? 0,
                    Status = e.Status,
                    AllocatedStaffPercentage = Math.Round(allocatedStaffPercentage, 2),
                    DescriptionUrl = e.Description_Url,
                    ImageUrl = e.Image_Url
                };
            }).ToList();
        }

        #endregion

        #region AllocateStaffToEventAsync

        public async Task<bool> AllocateStaffToEventAsync(int eventId, int employeeId)
        {
            // 1. Fetch event
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);
            if (ev == null)
            {
                throw new NotFoundException($"Event with ID {eventId} not found.");
            }

            // 2. Validate event type
            if (!string.Equals(ev.Event_Type, "Physical", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ev.Event_Type, "Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Staff allocation is only allowed for Physical or Hybrid events.");
            }

            // 3. Fetch staff
            var staff = await _staffRepository.GetByIdAsync(employeeId);
            if (staff == null)
            {
                throw new NotFoundException($"Staff member with ID {employeeId} not found.");
            }

            // 4. Validate working region matches event venue region
            if (ev.Venue == null)
            {
                throw new ValidationException("Event does not have a venue assigned.");
            }
            if (!string.Equals(staff.Region_Id, ev.Venue.Region_Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"Staff member works in region {staff.Region_Id} but event venue is in region {ev.Venue.Region_Id}.");
            }

            // 5. Check if staff is already allocated to this event
            var isAlreadyAllocated = ev.StaffAllocations.Any(sa => sa.Employee_ID == employeeId);
            if (isAlreadyAllocated)
            {
                throw new ValidationException("Staff member is already allocated to this event.");
            }

            // 6. Map staff to event
            var allocation = new EventStaffAllocation
            {
                Event_Id = eventId,
                Employee_ID = employeeId
            };
            ev.StaffAllocations.Add(allocation);
            await _eventRepository.UpdateAsync(ev);

            // 7. Update staff status to allocated
            staff.IsAllocated = true;
            await _staffRepository.UpdateAsync(staff);

            return true;
        }

        #endregion

        #region UpdateVenueAsync

        public async Task<VenueResponse> UpdateVenueAsync(int venueId, CreateVenueRequest request)
        {
            var venue = await _venueRepository.GetByIdAsync(venueId);
            if (venue == null)
                throw new NotFoundException($"Venue with ID {venueId} not found.");

            venue.Name = request.Name;
            venue.Address = request.Address;
            venue.Hourly_Price = request.Hourly_Price;
            venue.Region_Id = request.Region_Id;
            venue.Is_Available = request.Is_Available;

            await _venueRepository.UpdateAsync(venue);

            var updated = (await _venueRepository.GetAllWithDetailsAsync())
                .First(v => v.Venue_Id == venue.Venue_Id);

            return new VenueResponse
            {
                Venue_Id = updated.Venue_Id,
                Region_Id = updated.Region_Id,
                Name = updated.Name,
                Address = updated.Address,
                Hourly_Price = updated.Hourly_Price,
                Is_Available = updated.Is_Available,
                SeatTiers = updated.SeatCapacities.Select(sc => new SeatTierResponse
                {
                    Tier_Name = sc.Tier_Name,
                    Total_Seats = sc.Total_Seats
                }).ToList()
            };
        }

        #endregion

        #region AdminProfile

        public async Task<AdminProfileResponse> GetAdminProfileAsync(string adminId)
        {
            var admin = await _adminRepository.GetByAdminIdAsync(adminId);
            if (admin == null)
                throw new NotFoundException($"Admin with ID {adminId} not found.");

            return new AdminProfileResponse
            {
                Admin_Id = admin.Admin_Id,
                Name = admin.Name,
                Email = admin.Email
            };
        }

        public async Task<AdminProfileResponse> UpdateAdminProfileAsync(string adminId, UpdateAdminProfileRequest request)
        {
            var admin = await _adminRepository.GetByAdminIdAsync(adminId);
            if (admin == null)
                throw new NotFoundException($"Admin with ID {adminId} not found.");

            admin.Name = request.Name;
            await _adminRepository.UpdateAsync(admin);

            return new AdminProfileResponse
            {
                Admin_Id = admin.Admin_Id,
                Name = admin.Name,
                Email = admin.Email
            };
        }

        #endregion

        #region GetHelpdeskMetadataAsync

        public async Task<HelpdeskMetadataResponse> GetHelpdeskMetadataAsync()
        {
            string filePath = ResolveHelpdeskFilePath();

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var result = JsonSerializer.Deserialize<HelpdeskMetadataResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null && (result.Actions?.Count > 0 || result.TargetTypes?.Count > 0))
                    return result;
            }

            return new HelpdeskMetadataResponse
            {
                Actions = new List<HelpdeskAction>
                {
                    new() { Key = "REF", Label = "Refund" },
                    new() { Key = "EVT", Label = "Event" },
                    new() { Key = "ACC", Label = "Account" },
                    new() { Key = "GEN", Label = "General" }
                },
                TargetTypes = new List<HelpdeskTargetType>
                {
                    new() { Key = "ATD", Label = "Attendee" },
                    new() { Key = "ORG", Label = "Organizer" }
                }
            };
        }

        private static string ResolveHelpdeskFilePath()
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

            string primary = Path.Combine(rootPath, "Event.Business", "assets", "admin", "helpdesk-types.json");
            if (File.Exists(primary)) return primary;

            string binFallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "admin", "helpdesk-types.json");
            return binFallback;
        }

        #endregion

        #region GetAllVenuesIncludingInactiveAsync

        public async Task<IEnumerable<VenueResponse>> GetAllVenuesIncludingInactiveAsync()
        {
            var venues = await _venueRepository.GetAllWithDetailsAsync();
            return venues.Select(v => new VenueResponse
            {
                Venue_Id     = v.Venue_Id,
                Region_Id    = v.Region_Id,
                Name         = v.Name,
                Address      = v.Address,
                Hourly_Price = v.Hourly_Price,
                Is_Available = v.Is_Available,
                SeatTiers    = v.SeatCapacities.Select(sc => new SeatTierResponse
                {
                    Tier_Name   = sc.Tier_Name,
                    Total_Seats = sc.Total_Seats
                }).ToList()
            }).ToList();
        }

        #endregion

        #region UpdateEventVenueAsync

        public async Task<bool> UpdateEventVenueAsync(int eventId, int venueId)
        {
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);
            if (ev == null)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            var venue = await _venueRepository.GetByIdAsync(venueId);
            if (venue == null)
                throw new NotFoundException($"Venue with ID {venueId} not found.");

            ev.Venue_Id = venueId;
            await _eventRepository.UpdateAsync(ev);
            return true;
        }

        #endregion

        #region SearchGlobalAsync

        public async Task<object> SearchGlobalAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new { Events = new List<object>(), Bookings = new List<object>() };

            var kw = keyword.ToLower();
            
            var allEvents = await _eventRepository.GetAllAsync();
            var matchedEvents = allEvents.Where(e => 
                e.Event_Id.ToString().Contains(kw) || 
                (e.Title != null && e.Title.ToLower().Contains(kw)) || 
                (e.Venue != null && e.Venue.Name != null && e.Venue.Name.ToLower().Contains(kw))
            ).Select(e => new {
                EventId = e.Event_Id,
                Title = e.Title,
                VenueName = e.Venue?.Name,
                Status = e.Status,
                Date = e.Date_Time
            }).Take(20).ToList();

            var allBookings = await _bookingRepository.GetAllAsync();
            var matchedBookings = allBookings.Where(b => 
                b.Booking_Id.ToString().Contains(kw) ||
                b.Event_Id.ToString().Contains(kw)
            ).Select(b => new {
                BookingId = b.Booking_Id,
                EventId = b.Event_Id,
                Status = b.Booking_Status,
                Date = b.Created_At
            }).Take(20).ToList();

            return new {
                Events = matchedEvents,
                Bookings = matchedBookings
            };
        }

        #endregion
    }
}
