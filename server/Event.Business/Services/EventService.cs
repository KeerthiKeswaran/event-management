using System;
using System.Threading.Tasks;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using System.Linq;
using Serilog;
using Event.Business.Exceptions;
using Event.Models.DTOs;
using System.Collections.Generic;
using System.IO;
using Event.Business.Helpers;

namespace Event.Business.Services
{
    public class EventService : IEventService
    {
        #region Fields

        private readonly IEventRepository _eventRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly IPlatformSettingsRepository _settingsRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IPaymentService _paymentService;
        private readonly IOrganizerUpfrontPaymentRepository _upfrontPaymentRepository;
        private readonly IVirtualMeetingService _virtualMeetingService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IBookingPaymentRepository _bookingPaymentRepository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IRefundService _refundService;
        private readonly ITermsAndConditionsRepository _termsRepository;
        private readonly IOrganizerPayoutRepository _payoutRepository;

        // Thread-safe in-memory cache for calculations (temporary memory store)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Event.Models.DTOs.StaffAvailabilityResponse> _staffCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, Event.Models.DTOs.StaffAvailabilityResponse>();

        #endregion

        #region Constructor

        public EventService(
            IEventRepository eventRepository,
            IBookingRepository bookingRepository,
            IVenueRepository venueRepository,
            IPlatformSettingsRepository settingsRepository,
            IStaffRepository staffRepository,
            ITransactionRepository transactionRepository,
            IPaymentService paymentService,
            IOrganizerUpfrontPaymentRepository upfrontPaymentRepository,
            IVirtualMeetingService virtualMeetingService,
            INotificationRepository notificationRepository,
            IBookingPaymentRepository bookingPaymentRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            IRefundService refundService,
            ITermsAndConditionsRepository termsRepository,
            IOrganizerPayoutRepository payoutRepository)
        {
            _eventRepository = eventRepository;
            _bookingRepository = bookingRepository;
            _venueRepository = venueRepository;
            _settingsRepository = settingsRepository;
            _staffRepository = staffRepository;
            _transactionRepository = transactionRepository;
            _paymentService = paymentService;
            _upfrontPaymentRepository = upfrontPaymentRepository;
            _virtualMeetingService = virtualMeetingService;
            _notificationRepository = notificationRepository;
            _bookingPaymentRepository = bookingPaymentRepository;
            _emailService = emailService;
            _userRepository = userRepository;
            _refundService = refundService;
            _termsRepository = termsRepository;
            _payoutRepository = payoutRepository;
        }

        #endregion

        #region BrowseEventsAsync

        public async Task<PagedResult<BrowsedEventResponse>> BrowseEventsAsync(string? keyword, string? category, DateTime? minDateTime, string? regionId, string? format, decimal? maxPrice, string? sortBy, int page, int size)
        {
            // 1. Calculate the cutoff search time (must be at least 30 minutes in the future)
            var cutoffTime = DateTime.UtcNow.AddMinutes(30);
            var searchMinTime = minDateTime.HasValue && minDateTime.Value > cutoffTime
                ? minDateTime.Value
                : cutoffTime;

            // 2. Query event repository for paged results matching filters
            var rawResult = await _eventRepository.SearchEventsAsync(keyword, category, searchMinTime, regionId, format, maxPrice, sortBy, page, size);

            // 3. Map to BrowsedEventResponse DTO
            var mappedItems = new List<BrowsedEventResponse>();
            if (rawResult.Items != null)
            {
                foreach (var ev in rawResult.Items)
                {
                    var ticketTiers = new List<TicketTierDetailsDto>();
                    if (ev.TicketTiers != null)
                    {
                        foreach (var tier in ev.TicketTiers)
                        {
                            ticketTiers.Add(new TicketTierDetailsDto
                            {
                                Tier_Name = tier.Tier_Name,
                                Price = tier.Price,
                                Tickets_Sold = tier.Tickets_Sold,
                                Capacity = ev.Venue != null && ev.Venue.SeatCapacities != null 
                                            ? ev.Venue.SeatCapacities.FirstOrDefault(sc => sc.Tier_Name == tier.Tier_Name)?.Total_Seats ?? 99999
                                            : 99999
                            });
                        }
                    }

                    mappedItems.Add(new BrowsedEventResponse
                    {
                        Event_Id = ev.Event_Id,
                        Organizer_Name = ev.Organizer?.Name ?? string.Empty,
                        Organizer_Email = ev.Organizer?.Email,
                        Venue_Name = ev.Venue?.Name,
                        Address = ev.Venue?.Address,
                        Venue_Region_Name = ev.Venue?.Region?.Region_Name,
                        Event_Type = ev.Event_Type,
                        Category = ev.Category,
                        Title = ev.Title,
                        Description_Url = ev.Description_Url,
                        Image_Url = ev.Image_Url,
                        Date_Time = ev.Date_Time,
                        Status = ev.Status,
                        Duration_Hours = ev.Duration_Hours,
                        TicketTiers = ticketTiers
                    });
                }
            }

            var pagedResult = new PagedResult<BrowsedEventResponse>(mappedItems, rawResult.TotalCount, rawResult.Page, rawResult.PageSize)
            {
                MaxPrice = rawResult.MaxPrice
            };
            return pagedResult;
        }

        #endregion

        #region GetEventDetailsAsync

        public async Task<EventDetailsResponse?> GetEventDetailsAsync(int eventId, int? currentUserId = null)
        {
            // 1. Fetch details with eager loading of tiers and venue/seat capacities
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);

            // 2. Validate that event exists
            if (ev == null)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            if (ev.Status != "Live")
                throw new NotFoundException($"Event with ID {eventId} not found or is no longer available.");

            // 3. Map to DTO
            var ticketTiers = new List<TicketTierDetailsDto>();
            if (ev.TicketTiers != null)
            {
                foreach (var tier in ev.TicketTiers)
                {
                    ticketTiers.Add(new TicketTierDetailsDto
                    {
                        Tier_Name = tier.Tier_Name,
                        Price = tier.Price,
                        Tickets_Sold = tier.Tickets_Sold,
                        Capacity = ev.Venue != null && ev.Venue.SeatCapacities != null 
                                    ? ev.Venue.SeatCapacities.FirstOrDefault(sc => sc.Tier_Name == tier.Tier_Name)?.Total_Seats ?? 99999
                                    : 99999
                    });
                }
            }

            var organizerDto = new OrganizerDetailsDto
            {
                User_Id = ev.Organizer.User_Id,
                Name = ev.Organizer.Name,
                Email = ev.Organizer.Email
            };

            VenueDetailsDto? venueDto = null;
            if (ev.Venue != null)
            {
                venueDto = new VenueDetailsDto
                {
                    Region_Id = ev.Venue.Region_Id,
                    Name = ev.Venue.Name,
                    Address = ev.Venue.Address
                };
            }

            return new EventDetailsResponse
            {
                Event_Id = ev.Event_Id,
                Organizer_Id = ev.Organizer_Id,
                Organizer = organizerDto,
                Venue = venueDto,
                Event_Type = ev.Event_Type,
                Title = ev.Title,
                Category = ev.Category,
                Age_Category = ev.Age_Category,
                Description_Url = ev.Description_Url,
                Image_Url = ev.Image_Url,
                Date_Time = ev.Date_Time,
                Duration_Hours = ev.Duration_Hours,
                Status = ev.Status,
                TicketTiers = ticketTiers,
                Virtual_Url = ev.Virtual_Url,
                Virtual_Password_Hash = ev.Virtual_Password_Hash,
                Title_Update_Count = ev.Title_Update_Count,
                Has_Reported = currentUserId.HasValue ? await _eventRepository.HasUserReportedEventAsync(ev.Event_Id, currentUserId.Value) : (bool?)null
            };
        }

        #endregion

        #region ReportEventAsync

        public async Task<bool> ReportEventAsync(int reporterId, int eventId, string reason)
        {
            // 1. Verify event existence
            var evExists = await _eventRepository.ExistsAsync(eventId);
            if (!evExists)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            // 1.5 Check if user has already reported this event
            var hasReported = await _eventRepository.HasUserReportedEventAsync(eventId, reporterId);
            if (hasReported)
                throw new ValidationException("You have already reported this event.");

            // 2. Write report details to a JSON file
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

            string relativeDir = Path.Combine("users", reporterId.ToString(), "reports");
            string absoluteDir = Path.Combine(rootPath, folderName, "assets", relativeDir);
            if (!Directory.Exists(absoluteDir))
            {
                Directory.CreateDirectory(absoluteDir);
            }

            // 3. Create and save new event report in database
            var eventReport = new EventReport
            {
                Event_Id = eventId,
                Reporter_Id = reporterId,
                ReportUrl = $"/assets/{relativeDir}/report_pending.json",
                Created_At = DateTime.UtcNow
            };
            await _eventRepository.AddReportAsync(eventReport);

            string filename = $"report_{eventReport.Report_Id}.json";
            string absolutePath = Path.Combine(absoluteDir, filename);

            var reportData = new Dictionary<string, string>
            {
                { "Reason", reason }
            };
            string jsonText = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(absolutePath, jsonText);

            eventReport.ReportUrl = $"/assets/{relativeDir}/{filename}";
            await _eventRepository.UpdateReportAsync(eventReport);
            return true;
        }

        #endregion

        #region SubmitEventFeedbackAsync

        public async Task<bool> SubmitEventFeedbackAsync(int attendeeId, int eventId, int rating, string review)
        {
            // 1. Verify event existence
            var evExists = await _eventRepository.ExistsAsync(eventId);
            if (!evExists)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            // 2. Save feedback to a JSON file
            string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string feedbackDir = Path.Combine(rootPath, "Event.Business", "assets", "users", attendeeId.ToString(), "feedback");
            if (!Directory.Exists(feedbackDir))
            {
                Directory.CreateDirectory(feedbackDir);
            }
            string fileName = $"feedback_event_{eventId}_{DateTime.UtcNow.Ticks}.json";
            string filePath = Path.Combine(feedbackDir, fileName);
            
            var feedbackData = new { Rating = rating, Review = review, SubmittedAt = DateTime.UtcNow };
            await System.IO.File.WriteAllTextAsync(filePath, System.Text.Json.JsonSerializer.Serialize(feedbackData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            
            string reviewUrl = $"/assets/users/{attendeeId}/feedback/{fileName}";

            // 3. Create and save new attendee feedback
            var feedback = new EventFeedback
            {
                Attendee_Id = attendeeId,
                Event_Id = eventId,
                Rating = rating,
                Review = reviewUrl // Store the JSON URL here instead of raw text
            };
            await _eventRepository.AddFeedbackAsync(feedback);
            return true;
        }

        #endregion

        #region VerifyTicketCheckInAsync

        public async Task<Booking> VerifyTicketCheckInAsync(string secretHash)
        {
            if (string.IsNullOrWhiteSpace(secretHash))
                throw new ValidationException("Secret hash is required.");

            var booking = await _bookingRepository.GetBookingBySecretHashAsync(secretHash);
            if (booking == null)
                throw new NotFoundException("Booking not found for the provided QR code.");

            if (booking.Booking_Status != "Confirmed")
                throw new ValidationException($"Cannot check in. Booking status is '{booking.Booking_Status}'.");

            if (booking.CheckIn_Status == "Checked-In")
                throw new ValidationException("This ticket has already been checked in.");

            booking.CheckIn_Status = "Checked-In";
            await _bookingRepository.UpdateAsync(booking);
            return booking;
        }

        #endregion

        #region CheckStaffAvailabilityAsync

        public async Task<Event.Models.DTOs.StaffAvailabilityResponse> CheckStaffAvailabilityAsync(Event.Models.DTOs.CheckStaffAvailabilityRequest request)
        {
            var venue = await _venueRepository.GetByIdAsync(request.VenueId);
            if (venue == null || !venue.Is_Available)
                throw new NotFoundException("Venue not found or is currently unavailable.");

            var settings = await _settingsRepository.GetSettingsAsync()
                ?? throw new ValidationException("Platform settings are not configured.");

            int requiredStaff = CalculateRequiredStaffCount(venue);
            int availableStaff = await _staffRepository.GetAvailableStaffCountAsync(venue.Region_Id, request.DateTime);

            var response = new Event.Models.DTOs.StaffAvailabilityResponse
            {
                RequiredStaffCount = requiredStaff,
                AvailableStaffCount = availableStaff
            };

            // Rule 1: If available staff is less than 2, return 0 staff (none available)
            if (availableStaff < 2)
            {
                response.AvailableStaffCount = 0;
                response.StaffingCost = 0;
                response.IsAdequate = false;
                response.Message = "No support staff are available (minimum pool requirement of 2 staff not met).";
            }
            // Rule 2: If available is more than 2, but still less than the requirement
            else if (availableStaff < requiredStaff)
            {
                response.StaffingCost = settings.Staff_Flat_Rate * availableStaff * request.DurationHours;
                response.IsAdequate = false;
                response.Message = $"Partial staff available ({availableStaff} of {requiredStaff} required).";
            }
            else
            {
                response.StaffingCost = settings.Staff_Flat_Rate * requiredStaff * request.DurationHours;
                response.IsAdequate = true;
                response.Message = "Sufficient support staff are available.";
            }

            // Cache the result in temporary memory
            string cacheKey = $"{request.VenueId}_{request.DateTime:yyyyMMddHHmmss}";
            _staffCache[cacheKey] = response;

            return response;
        }

        #endregion

        #region CreateEventAsync

        public async Task<EventDetailsResponse> CreateEventAsync(int organizerId, Event.Models.DTOs.CreateEventRequest request)
        {
            // 0. Validation: Policy acceptance
            var activeEventPolicy = await _termsRepository.GetActiveTermsByTypeAsync("EventCreation");
            if (activeEventPolicy == null)
                throw new ValidationException("No active Event Creation policy is defined on the platform.");

            if (string.IsNullOrWhiteSpace(request.AcceptedPolicyId) || request.AcceptedPolicyId != activeEventPolicy.Terms_Id)
                throw new ValidationException("You must accept the latest event creation policy agreement before creating an event.");

            // 1. Validation: Event type
            if (string.IsNullOrWhiteSpace(request.EventType))
                throw new ValidationException("Event type is required.");

            if (string.IsNullOrWhiteSpace(request.Category))
                throw new ValidationException("Event category is required.");

            if (string.IsNullOrWhiteSpace(request.AgeCategory) || !(request.AgeCategory == "ALL" || request.AgeCategory == "KID" || request.AgeCategory == "ADL"))
                throw new ValidationException("Invalid age category. Must be one of: ALL, KID, ADL.");

            // 2. Validation: Event date must be at least 24 hours in the future
            if (request.DateTime < DateTime.UtcNow.AddHours(24))
                throw new ValidationException("Events must be scheduled at least 24 hours in the future.");

            // Check organizer status
            var organizer = await _userRepository.GetByIdAsync(organizerId);
            if (organizer == null)
                throw new NotFoundException($"Organizer with ID {organizerId} not found.");

            if (string.Equals(organizer.Status, "Restricted", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Your account is Restricted. You are disabled from creating further events.");

            // Append the policy ID to Consented_Terms_Id if not already present
            if (string.IsNullOrEmpty(organizer.Consented_Terms_Id))
            {
                organizer.Consented_Terms_Id = activeEventPolicy.Terms_Id;
                await _userRepository.UpdateAsync(organizer);
            }
            else if (!organizer.Consented_Terms_Id.Contains(activeEventPolicy.Terms_Id))
            {
                organizer.Consented_Terms_Id += activeEventPolicy.Terms_Id;
                await _userRepository.UpdateAsync(organizer);
            }

            if (string.Equals(organizer.Status, "Deactivated", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Your account is Deactivated.");

            // 3. Retrieve platform settings
            var settings = await _settingsRepository.GetSettingsAsync()
                ?? throw new ValidationException("Platform settings are not configured.");

            decimal upfrontFee = 0;
            Venue? venue = null;

            // 4. Physical / Hybrid validation
            if (request.EventType.Equals("Physical", StringComparison.OrdinalIgnoreCase) ||
                request.EventType.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                if (!request.VenueId.HasValue)
                    throw new ValidationException("Venue ID is required for Physical or Hybrid events.");

                venue = await _venueRepository.GetByIdAsync(request.VenueId.Value);
                if (venue == null || !venue.Is_Available)
                    throw new NotFoundException("Venue not found or is currently unavailable.");

                // Check venue occupancy
                bool isOccupied = await _venueRepository.IsVenueOccupiedAsync(venue.Venue_Id, request.DateTime);
                if (isOccupied)
                    throw new ConflictException("The selected venue is already booked for this date and time.");

                // Calculate venue rental cost
                decimal venueCost = venue.Hourly_Price * request.DurationHours;
                upfrontFee += settings.Physical_Event_Activation_Fee + venueCost;

                // Add virtual fee if it's a Hybrid event
                if (request.EventType.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    upfrontFee += settings.Virtual_Event_Activation_Fee;
                }

                // Check and calculate staff availability if requested by the organizer
                if (request.RequiresStaff)
                {
                    // Look up from temporary cache memory first, otherwise compute
                    string cacheKey = $"{request.VenueId.Value}_{request.DateTime:yyyyMMddHHmmss}";
                    if (_staffCache.TryGetValue(cacheKey, out var cachedResult))
                    {
                        if (cachedResult.AvailableStaffCount == 0)
                        {
                            throw new ConflictException("Cannot book staff. No support staff are available in the region.");
                        }

                        // Add staff cost based on actual allocated or available count (or whatever we can allocate)
                        upfrontFee += cachedResult.StaffingCost;
                    }
                    else
                    {
                        int requiredStaffCount = CalculateRequiredStaffCount(venue);
                        int availableStaffCount = await _staffRepository.GetAvailableStaffCountAsync(venue.Region_Id, request.DateTime);

                        if (availableStaffCount < 2)
                        {
                            throw new ConflictException("Cannot book staff. No support staff are available in the region.");
                        }

                        int allocatedCount = Math.Min(requiredStaffCount, availableStaffCount);
                        upfrontFee += settings.Staff_Flat_Rate * allocatedCount * request.DurationHours;
                    }
                }
            }
            else if (request.EventType.Equals("Virtual", StringComparison.OrdinalIgnoreCase))
            {
                upfrontFee += settings.Virtual_Event_Activation_Fee;
            }
            else
            {
                throw new ValidationException("Invalid event type. Must be Physical, Virtual, or Hybrid.");
            }

            // Apply GST (calculate authoritative total payment amount)
            decimal gstAmount = upfrontFee * (settings.GST_Percentage / 100m);
            upfrontFee += gstAmount;

            // Begin transaction
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Create Event entity (Starts as 'Activation Pending' until upfront payment is completed)
                var newEvent = new Event.Models.Event
                {
                    Organizer_Id = organizerId,
                    Venue_Id = request.VenueId,
                    Event_Type = request.EventType,
                    Title = request.Title,
                    Category = request.Category,
                    Age_Category = request.AgeCategory,
                    Description_Url = request.DescriptionUrl,
                    Image_Url = request.ImageUrl,
                    Date_Time = request.DateTime,
                    Duration_Hours = request.DurationHours,
                    Status = "Activation Pending",
                    Requires_Staff = request.RequiresStaff,
                    Virtual_Url = null,
                    Virtual_Password_Hash = null
                };

                // Add ticket tiers
                foreach (var tier in request.TicketTiers)
                {
                    newEvent.TicketTiers.Add(new EventTicketTier
                    {
                        Tier_Name = tier.TierName,
                        Price = tier.Price,
                        Tickets_Sold = 0
                    });
                }

                await _eventRepository.AddAsync(newEvent);

                // Move temporary files to permanent event directory
                bool filesMoved = false;
                var baseAssetsPath = GetBaseAssetsPath();
                var eventAssetsDir = Path.Combine(baseAssetsPath, "events", newEvent.Event_Id.ToString());

                if (!string.IsNullOrEmpty(newEvent.Image_Url) && newEvent.Image_Url.Contains("/temp/"))
                {
                    // Extract relative path from URL (e.g. "assets/events/temp/guid/image.png")
                    var relativePath = newEvent.Image_Url.Replace("assets/", "").TrimStart('/');
                    var tempFilePath = Path.Combine(baseAssetsPath, relativePath);
                    
                    if (File.Exists(tempFilePath))
                    {
                        Directory.CreateDirectory(eventAssetsDir);
                        var ext = Path.GetExtension(tempFilePath);
                        // User specifically requested cover.png
                        var finalImagePath = Path.Combine(eventAssetsDir, $"cover{ext}");
                        File.Move(tempFilePath, finalImagePath, true);
                        newEvent.Image_Url = $"assets/events/{newEvent.Event_Id}/cover{ext}";
                        filesMoved = true;
                    }
                }

                if (!string.IsNullOrEmpty(newEvent.Description_Url) && newEvent.Description_Url.Contains("/temp/"))
                {
                    var relativePath = newEvent.Description_Url.Replace("assets/", "").TrimStart('/');
                    var tempFilePath = Path.Combine(baseAssetsPath, relativePath);
                    
                    if (File.Exists(tempFilePath))
                    {
                        Directory.CreateDirectory(eventAssetsDir);
                        var finalDescPath = Path.Combine(eventAssetsDir, "description.txt");
                        File.Move(tempFilePath, finalDescPath, true);
                        newEvent.Description_Url = $"assets/events/{newEvent.Event_Id}/description.txt";
                        filesMoved = true;
                    }
                }

                if (filesMoved)
                {
                    await _eventRepository.UpdateAsync(newEvent);
                }

                if (venue != null)
                {
                    venue.Is_Available = false;
                    await _venueRepository.UpdateAsync(venue);
                }

                // Create ledger transaction for upfront payment
                var transaction = new Transaction
                {
                    Sender_Id = $"Organizer_User_{organizerId}",
                    Receiver_Id = "Platform_Escrow",
                    Transaction_Type = "OrganizerUpfrontPayment",
                    Related_Id = newEvent.Event_Id,
                    Amount = upfrontFee,
                    Currency = "INR",
                    Status = "Pending",
                    Created_At = DateTime.UtcNow,
                    Remarks = $"Upfront payment for publishing Event '{request.Title}'"
                };

                await _transactionRepository.AddAsync(transaction);

                await _bookingRepository.CommitTransactionAsync();

                // Map to DTO
                var ticketTiers = new List<TicketTierDetailsDto>();
                if (newEvent.TicketTiers != null)
                {
                    foreach (var tier in newEvent.TicketTiers)
                    {
                        ticketTiers.Add(new TicketTierDetailsDto
                        {
                            Tier_Name = tier.Tier_Name,
                            Price = tier.Price,
                            Tickets_Sold = tier.Tickets_Sold
                        });
                    }
                }

                var organizerDto = new OrganizerDetailsDto
                {
                    User_Id = organizer.User_Id,
                    Name = organizer.Name,
                    Email = organizer.Email
                };

                VenueDetailsDto? venueDto = null;
                if (venue != null)
                {
                    venueDto = new VenueDetailsDto
                    {
                        Region_Id = venue.Region_Id,
                        Name = venue.Name,
                        Address = venue.Address
                    };
                }

                return new EventDetailsResponse
                {
                    Event_Id = newEvent.Event_Id,
                    Organizer_Id = newEvent.Organizer_Id,
                    Organizer = organizerDto,
                    Venue = venueDto,
                    Event_Type = newEvent.Event_Type,
                    Title = newEvent.Title,
                    Category = newEvent.Category,
                    Age_Category = newEvent.Age_Category,
                    Description_Url = newEvent.Description_Url,
                    Image_Url = newEvent.Image_Url,
                    Date_Time = newEvent.Date_Time,
                    Duration_Hours = newEvent.Duration_Hours,
                    Status = newEvent.Status,
                    TicketTiers = ticketTiers
                };
            }
            catch (Exception)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region CreateCheckoutSessionForEventCreationAsync

        public async Task<(bool Success, string SessionId, string ClientSecret, System.DateTime CreatedAtUTC, string ErrorMessage)> CreateCheckoutSessionForEventCreationAsync(int eventId, string returnUrl)
        {
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);
            if (ev == null)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            if (ev.Status != "Activation Pending")
                throw new ValidationException($"Event is already in '{ev.Status}' status.");

            var transaction = await _transactionRepository.GetPendingOrganizerUpfrontTransactionAsync(eventId);
            if (transaction == null)
                throw new NotFoundException("Pending upfront payment transaction not found for this event.");

            string itemName = $"Event Activation: {ev.Title}";

            return await _paymentService.CreateCheckoutSessionAsync(transaction.Amount, transaction.Currency, itemName, returnUrl);
        }

        #endregion

        #region ConfirmEventUpfrontPaymentAsync

        public async Task<EventDetailsResponse> ConfirmEventUpfrontPaymentAsync(int eventId, string stripeChargeId, string paymentMethod)
        {
            // Step 1: Start a new database transaction to guarantee database consistency and atomicity.
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Step 2: Retrieve the event details, eager loading the venue, capacities, and ticket tiers.
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                // Step 3: Validate that the event status is currently in 'Activation Pending' status.
                if (ev.Status != "Activation Pending")
                    throw new ValidationException($"Event is already in '{ev.Status}' status.");

                // Step 4: Fetch the associated pending upfront payment transaction from the database ledger.
                var transaction = await _transactionRepository.GetPendingOrganizerUpfrontTransactionAsync(eventId);
                if (transaction == null)
                    throw new NotFoundException("Pending upfront payment transaction not found for this event.");

                // Step 5: Charge the organizer's card using the payment gateway service (Stripe).
                (bool Success, string TransactionReference, string ErrorMessage) paymentResult;
                if (paymentMethod == "stripe_checkout")
                {
                    var sessionService = new Stripe.Checkout.SessionService();
                    var session = await sessionService.GetAsync(stripeChargeId);
                    if (session.PaymentStatus == "paid")
                    {
                        paymentResult = (true, session.PaymentIntentId ?? session.Id, string.Empty);
                    }
                    else
                    {
                        paymentResult = (false, string.Empty, $"Checkout session payment status is {session.PaymentStatus}");
                    }
                }
                else
                {
                    paymentResult = await _paymentService.CreateChargeAsync(
                        transaction.Amount,
                        transaction.Currency,
                        stripeChargeId,
                        $"Organizer Upfront Payment for Event #{eventId}: {ev.Title}");
                }

                // Step 6: Handle payment failures by updating transaction logs and committing the failed state.
                if (!paymentResult.Success)
                {
                    transaction.Status = "Failed";
                    transaction.Remarks = paymentResult.ErrorMessage;
                    await _transactionRepository.UpdateAsync(transaction);
                    await _bookingRepository.CommitTransactionAsync();

                    try
                    {
                        await RevertPendingEventCreationAsync(eventId);
                    }
                    catch (Exception)
                    {
                        // Ignore inner exception to propagate the original charge failed validation exception
                    }

                    throw new ValidationException($"Stripe payment failed: {paymentResult.ErrorMessage}");
                }

                // Step 7: Update the transaction log to record successful checkout parameters.
                transaction.Status = "Success";
                transaction.Transaction_Reference = paymentResult.TransactionReference;
                transaction.Payment_Method_Details = paymentMethod;
                await _transactionRepository.UpdateAsync(transaction);

                // Step 8: Log a successful record in the OrganizerUpfrontPayments mapping table.
                var upfrontPayment = new OrganizerUpfrontPayment
                {
                    Event_Id = eventId,
                    Transaction_Id = transaction.Transaction_Id,
                    Amount = transaction.Amount,
                    Payment_Status = "Success",
                    Created_At = DateTime.UtcNow
                };
                await _upfrontPaymentRepository.AddAsync(upfrontPayment);

                // Step 9: Set the main Event Status to 'Live' indicating that it is active and viewable by users.
                ev.Status = "Live";

                // Step 10: For Physical or Hybrid events, allocate support staffs.
                if (ev.Event_Type.Equals("Physical", StringComparison.OrdinalIgnoreCase) ||
                    ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    if (ev.Requires_Staff && ev.Venue != null)
                    {
                        // Calculate required staff using the helper method
                        int requiredStaffCount = CalculateRequiredStaffCount(ev.Venue);

                        // Query available support staff members in that region
                        var availableStaffs = await _staffRepository.GetAvailableStaffsAsync(ev.Venue.Region_Id, ev.Date_Time);
                        var staffsToAllocate = availableStaffs.Take(requiredStaffCount).ToList();

                        // Save allocations and update staff availability status
                        foreach (var staff in staffsToAllocate)
                        {
                            ev.StaffAllocations.Add(new EventStaffAllocation
                            {
                                Event_Id = ev.Event_Id,
                                Employee_ID = staff.Employee_ID
                            });

                            staff.IsAllocated = true;
                            await _staffRepository.UpdateAsync(staff);
                        }
                    }
                }

                // Step 11: For Virtual or Hybrid events, generate a Jitsi Meet meeting link and password hash.
                string? generatedPasscode = null;
                if (ev.Event_Type.Equals("Virtual", StringComparison.OrdinalIgnoreCase) ||
                    ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    var (roomUrl, rawPasscode) = await _virtualMeetingService.GenerateMeetingRoomAsync(ev.Title);
                    ev.Virtual_Url = roomUrl;
                    ev.Virtual_Password_Hash = Event.Business.Helpers.CryptoHelper.Encrypt(rawPasscode);
                    generatedPasscode = rawPasscode;

                    transaction.Remarks += $"\n[Virtual Access Passcode]: {rawPasscode}";
                    await _transactionRepository.UpdateAsync(transaction);
                }

                // Step 12: Persist all updated properties on the Event object.
                await _eventRepository.UpdateAsync(ev);

                // Step 12.5: Enqueue email notification to organizer
                if (ev.Organizer != null && !string.IsNullOrEmpty(ev.Organizer.Email))
                {
                    try
                    {
                        string locationDetails = "";
                        string linkStyle = "color: #ffffff; text-decoration: underline;";
                        string virtualInfo = $"<a href='{ev.Virtual_Url}' style='{linkStyle}'>{ev.Virtual_Url}</a> <br/> Password: {generatedPasscode} <br/> <em>(Please maintain this password confidential)</em>";

                        if (ev.Event_Type.Equals("Virtual", StringComparison.OrdinalIgnoreCase))
                            locationDetails = ev.Virtual_Url != null ? virtualInfo : "Link TBD";
                        else if (ev.Event_Type.Equals("Physical", StringComparison.OrdinalIgnoreCase))
                            locationDetails = ev.Venue?.Name ?? "Venue TBD";
                        else if (ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                            locationDetails = $"{ev.Venue?.Name ?? "Venue TBD"} (Physical) <br/> {(ev.Virtual_Url != null ? virtualInfo : "Link TBD")} (Virtual)";

                        var emailDto = new EmailTemplateDto
                        {
                            TemplateName = "EventCreationSuccessTemplate.html",
                            Placeholders = new Dictionary<string, string>
                            {
                                { "title", ev.Title },
                                { "dateTime", ev.Date_Time.ToString("f") },
                                { "eventType", ev.Event_Type },
                                { "locationDetails", locationDetails },
                                { "upfrontFee", $"INR {transaction.Amount:N2}" },
                                { "year", DateTime.UtcNow.Year.ToString() }
                            }
                        };
                        string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);
                        await NotificationHelper.SendAndSaveNotificationAsync(
                            _notificationRepository,
                            _emailService,
                            ev.Organizer.Email,
                            $"Event Activated Successfully: {ev.Title}",
                            htmlBody
                        );
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to send and queue success email for event creation {EventId}", ev.Event_Id);
                    }
                }

                // Step 13: Commit the database transaction to apply all modifications atomically.
                await _bookingRepository.CommitTransactionAsync();

                // Map to DTO
                var ticketTiers = new List<TicketTierDetailsDto>();
                if (ev.TicketTiers != null)
                {
                    foreach (var tier in ev.TicketTiers)
                    {
                        ticketTiers.Add(new TicketTierDetailsDto
                        {
                            Tier_Name = tier.Tier_Name,
                            Price = tier.Price,
                            Tickets_Sold = tier.Tickets_Sold
                        });
                    }
                }

                var organizerDto = new OrganizerDetailsDto
                {
                    User_Id = ev.Organizer.User_Id,
                    Name = ev.Organizer.Name,
                    Email = ev.Organizer.Email
                };

                VenueDetailsDto? venueDto = null;
                if (ev.Venue != null)
                {
                    venueDto = new VenueDetailsDto
                    {
                        Region_Id = ev.Venue.Region_Id,
                        Name = ev.Venue.Name,
                        Address = ev.Venue.Address
                    };
                }

                return new EventDetailsResponse
                {
                    Event_Id = ev.Event_Id,
                    Organizer_Id = ev.Organizer_Id,
                    Organizer = organizerDto,
                    Venue = venueDto,
                    Event_Type = ev.Event_Type,
                    Title = ev.Title,
                    Category = ev.Category,
                    Age_Category = ev.Age_Category,
                    Description_Url = ev.Description_Url,
                    Image_Url = ev.Image_Url,
                    Date_Time = ev.Date_Time,
                    Duration_Hours = ev.Duration_Hours,
                    Status = ev.Status,
                    TicketTiers = ticketTiers
                };
            }
            catch (Exception)
            {
                // Step 14: Roll back the transaction if any processing step throws an exception.
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region ReleaseExpiredEventCreationAsync

        public async Task ReleaseExpiredEventCreationAsync()
        {
            // Step 1: Configure Serilog file logger pointing to logs/business.log.
            var logger = new Serilog.LoggerConfiguration()
                .WriteTo.File("logs/business.log", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            // Step 2: Establish the 5-minute event creation expiration cutoff.
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
            logger.Information("ReleaseExpiredEventCreationAsync job started at {Time}. Cutoff time is {CutoffTime}.", DateTime.UtcNow, cutoffTime);

            try
            {
                // Step 3: Fetch all expired 'Activation Pending' events from database.
                var expiredEvents = await _eventRepository.GetExpiredEventsAsync(cutoffTime);
                int count = 0;

                // Step 4: Loop through each expired event to roll back settings.
                foreach (var ev in expiredEvents)
                {
                    try
                    {
                        logger.Information("Expiring unconfirmed Event ID {EventId}: '{Title}' created by Organizer ID {OrganizerId}.", ev.Event_Id, ev.Title, ev.Organizer_Id);
                        await RevertPendingEventCreationAsync(ev.Event_Id);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to release expired Event ID {EventId}.", ev.Event_Id);
                    }
                }

                logger.Information("ReleaseExpiredEventCreationAsync job completed. Total events expired: {Count}.", count);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred during the ReleaseExpiredEventCreationAsync background job execution.");
            }
        }

        #endregion

        #region ReleaseCompletedEventsAsync

        public async Task ReleaseCompletedEventsAsync()
        {
            // Configure Serilog file logger pointing to logs/business.log.
            var logger = new Serilog.LoggerConfiguration()
                .WriteTo.File("logs/business.log", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            logger.Information("ReleaseCompletedEventsAsync job started at {Time}.", DateTime.UtcNow);

            try
            {
                // 1. Fetch all live events
                var liveEvents = await _eventRepository.GetLiveEventsWithDetailsAsync();
                var now = DateTime.UtcNow;

                // Filter live events whose end time has passed
                var completedEvents = liveEvents
                    .Where(e => e.Status == "Live" && e.Date_Time.AddHours((double)e.Duration_Hours) <= now)
                    .ToList();

                logger.Information("Found {Count} completed events to release.", completedEvents.Count);

                foreach (var ev in completedEvents)
                {
                    await _bookingRepository.BeginTransactionAsync();
                    try
                    {
                        logger.Information("Processing completion for Event ID {EventId}: '{Title}'...", ev.Event_Id, ev.Title);

                        // a. Update Event Status
                        ev.Status = "Completed";

                        // b. Revoke Virtual/Hybrid Meeting Access
                        ev.Virtual_Url = "Disabled";
                        ev.Virtual_Password_Hash = null;

                        await _eventRepository.UpdateAsync(ev);

                        // c. Release & Deallocate Support Staff
                        foreach (var allocation in ev.StaffAllocations)
                        {
                            var staff = await _staffRepository.GetByIdAsync(allocation.Employee_ID);
                            if (staff != null)
                            {
                                staff.IsAllocated = false;
                                await _staffRepository.UpdateAsync(staff);
                            }
                        }

                        // d. Fetch bookings to calculate financial totals and trigger notifications
                        var bookings = await _bookingRepository.GetBookingsByEventIdAsync(ev.Event_Id);

                        decimal totalTicketSales = 0m;
                        decimal platformCommission = 0m;

                        foreach (var booking in bookings)
                        {
                            if (booking.Booking_Status == "Confirmed")
                            {
                                // Revoke booking virtual url
                                booking.Virtual_Url = "Disabled";
                                await _bookingRepository.UpdateAsync(booking);

                                // Load success payment to aggregate financials
                                var successPayment = booking.Payments?.FirstOrDefault(p => p.Payment_Status == "Success");
                                if (successPayment != null)
                                {
                                    totalTicketSales += successPayment.Amount;
                                    platformCommission += successPayment.Platform_Fee_Cut;
                                }

                                // Trigger email / notification invitation to submit feedback
                                var attendeeEmail = booking.Attendee?.Email;
                                if (!string.IsNullOrEmpty(attendeeEmail))
                                {
                                    try
                                    {
                                        var feedbackEmailDto = new Event.Models.DTOs.EmailTemplateDto
                                        {
                                            TemplateName = "EventCompletionTemplate.html",
                                            Placeholders = new Dictionary<string, string>
                                            {
                                                { "eventName", ev.Title },
                                                { "year", DateTime.UtcNow.Year.ToString() }
                                            }
                                        };
                                        string htmlFeedbackBody = await _emailService.BuildEmailHtmlAsync(feedbackEmailDto);
                                        await NotificationHelper.SendAndSaveNotificationAsync(
                                            _notificationRepository,
                                            _emailService,
                                            attendeeEmail,
                                            $"We value your feedback: {ev.Title}",
                                            htmlFeedbackBody
                                        );
                                    }
                                    catch (Exception emailEx)
                                    {
                                        logger.Error(emailEx, "Failed to send feedback invitation email to {Email} for Event ID {EventId}.", attendeeEmail, ev.Event_Id);
                                    }
                                }
                            }
                        }

                        decimal payoutAmount = totalTicketSales - platformCommission;

                        // e. Organizer Payout Rule
                        // "Organizer payout, it must be cancelled if the event have any report and the response action is null/uphold by the admin.
                        // only if the event does't have any reports or the response action is dismissed then it must proceed with the payout."
                        var allReports = await _eventRepository.GetAllReportsAsync();
                        var eventReports = allReports.Where(r => r.Event_Id == ev.Event_Id).ToList();

                        bool hasActiveOrUpholdReports = eventReports.Any(r => r.ResponseAction == null || r.ResponseAction == "Upholds");

                        string payoutStatus = hasActiveOrUpholdReports ? "Cancelled" : "Success";
                        string transactionStatus = hasActiveOrUpholdReports ? "Cancelled" : "Success";
                        string payoutRemarks = hasActiveOrUpholdReports
                            ? $"Organizer payout cancelled due to active policy reports against event #{ev.Event_Id}."
                            : $"Payout processed successfully for completed event #{ev.Event_Id}.";

                        // Create Transaction entry
                        var payoutTx = new Transaction
                        {
                            Sender_Id = "Platform_Escrow",
                            Receiver_Id = $"Organizer_User_{ev.Organizer_Id}",
                            Transaction_Type = "OrganizerPayout",
                            Related_Id = ev.Event_Id,
                            Amount = payoutAmount,
                            Currency = "INR",
                            Status = transactionStatus,
                            Created_At = DateTime.UtcNow,
                            Remarks = payoutRemarks
                        };
                        await _transactionRepository.AddAsync(payoutTx);

                        // Create OrganizerPayout record
                        var organizerPayout = new OrganizerPayout
                        {
                            Event_Id = ev.Event_Id,
                            Transaction_Id = payoutTx.Transaction_Id,
                            Total_Ticket_Sales = totalTicketSales,
                            Platform_Commission = platformCommission,
                            Payout_Amount = payoutAmount,
                            Payout_Status = payoutStatus,
                            Processed_At = DateTime.UtcNow
                        };
                        await _payoutRepository.AddAsync(organizerPayout);

                        await _bookingRepository.CommitTransactionAsync();
                        logger.Information("Completed processing for Event ID {EventId}.", ev.Event_Id);
                    }
                    catch (Exception ex)
                    {
                        await _bookingRepository.RollbackTransactionAsync();
                        logger.Error(ex, "Failed to complete Event ID {EventId}.", ev.Event_Id);
                    }
                }

                logger.Information("ReleaseCompletedEventsAsync job completed.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred during the ReleaseCompletedEventsAsync background job execution.");
            }
        }

        public async Task ProcessDismissedPayoutsAsync()
        {
            var logger = new Serilog.LoggerConfiguration()
                .WriteTo.File("logs/business.log", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            logger.Information("ProcessDismissedPayoutsAsync job started.");

            try
            {
                var allEvents = await _eventRepository.GetAllAsync();
                var completedEvents = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(allEvents, e => e.Status == "Completed"));

                logger.Information("Checking {Count} completed events for dismissed report payouts.", completedEvents.Count);

                foreach (var ev in completedEvents)
                {
                    var payout = await _payoutRepository.GetPayoutByEventIdAsync(ev.Event_Id);
                    if (payout != null && payout.Payout_Status == "Cancelled")
                    {
                        var allReports = await _eventRepository.GetAllReportsAsync();
                        var eventReports = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(allReports, r => r.Event_Id == ev.Event_Id));

                        // Only process if reports exist and all of them are dismissed
                        if (eventReports.Count > 0 && System.Linq.Enumerable.All(eventReports, r => r.ResponseAction == "Dismissed"))
                        {
                            logger.Information("Releasing payout for completed Event ID {EventId} (all reports are Dismissed).", ev.Event_Id);

                            await _bookingRepository.BeginTransactionAsync();
                            try
                            {
                                payout.Payout_Status = "Success";
                                payout.Processed_At = DateTime.UtcNow;
                                await _payoutRepository.UpdateAsync(payout);

                                var txs = await _transactionRepository.GetAllAsync();
                                var relatedTx = System.Linq.Enumerable.FirstOrDefault(txs, t => t.Transaction_Type == "OrganizerPayout" && t.Related_Id == ev.Event_Id);
                                if (relatedTx != null)
                                {
                                    relatedTx.Status = "Success";
                                    relatedTx.Remarks = "Payout released to organizer after reports were dismissed.";
                                    await _transactionRepository.UpdateAsync(relatedTx);
                                }

                                await _bookingRepository.CommitTransactionAsync();
                                logger.Information("Successfully released payout for Event ID {EventId}.", ev.Event_Id);
                            }
                            catch (Exception ex)
                            {
                                await _bookingRepository.RollbackTransactionAsync();
                                logger.Error(ex, "Failed to release payout for Event ID {EventId}.", ev.Event_Id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in ProcessDismissedPayoutsAsync job.");
            }
        }

        #endregion

        #region CancelEventAsync

        public async Task<bool> CancelEventAsync(int eventId, string refundType = "Dynamic", string cancellationMessage = "We regret to inform you that the event you booked has been cancelled by the organizer.")
        {
            // Step 1: Start database transaction boundaries.
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Step 2: Retrieve event details (with ticket tiers and venues).
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                // Step 3: Prevent duplicate cancellations.
                if (ev.Status == "Cancelled")
                    throw new ValidationException("Event is already cancelled.");

                // Step 4: Process organizer and attendee refunds using RefundService
                var (organizerRefundAmount, organizerRemarks, attendeeRefundResults) = await _refundService.RefundOrganizerAsync(eventId, refundType);

                // Step 5: Retrieve all bookings made for this event.
                var bookings = await _bookingRepository.GetBookingsByEventIdAsync(eventId);

                // Step 6: Release and free up allocated support staff members.
                foreach (var allocation in ev.StaffAllocations)
                {
                    var staff = await _staffRepository.GetByIdAsync(allocation.Employee_ID);
                    if (staff != null)
                    {
                        staff.IsAllocated = false;
                        await _staffRepository.UpdateAsync(staff);
                    }
                }

                // Step 6.5: Release the allocated venue if applicable.
                if ((ev.Event_Type == "Physical" || ev.Event_Type == "Hybrid") && ev.Venue_Id.HasValue)
                {
                    var venue = await _venueRepository.GetByIdAsync(ev.Venue_Id.Value);
                    if (venue != null)
                    {
                        venue.Is_Available = true;
                        await _venueRepository.UpdateAsync(venue);
                    }
                }

                // Step 7: Commit transaction to apply changes.
                await _bookingRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                // Step 8: Roll back on exception.
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region RevertPendingEventCreationAsync

        public async Task<bool> RevertPendingEventCreationAsync(int eventId)
        {
            // Step 1: Start transaction for database safety.
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Step 2: Fetch the event.
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                // Step 3: Validate that the event is in "Activation Pending" status.
                if (ev.Status != "Activation Pending")
                    throw new ValidationException($"Event cannot be reverted. Current status is '{ev.Status}'.");

                // Step 4: Find the associated pending activation payment ledger record and set it to Failed.
                var transaction = await _transactionRepository.GetPendingOrganizerUpfrontTransactionAsync(eventId);
                if (transaction != null)
                {
                    transaction.Status = "Failed";
                    transaction.Remarks = "Event creation pending payment reverted/cancelled by the organizer.";
                    await _transactionRepository.UpdateAsync(transaction);
                }

                // Step 5: Soft delete by setting event status to Failed.
                ev.Status = "Failed";
                await _eventRepository.UpdateAsync(ev);

                // Step 5.5: Update venue availability if it's not occupied by another active event.
                if (ev.Venue_Id.HasValue)
                {
                    bool isOccupied = await _venueRepository.IsVenueOccupiedAsync(ev.Venue_Id.Value, ev.Date_Time);
                    if (!isOccupied)
                    {
                        var venueToRelease = await _venueRepository.GetByIdAsync(ev.Venue_Id.Value);
                        if (venueToRelease != null)
                        {
                            venueToRelease.Is_Available = true;
                            await _venueRepository.UpdateAsync(venueToRelease);
                        }
                    }
                }

                // Step 6: Commit transaction.
                await _bookingRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                // Step 7: Rollback on error.
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region CalculateRequiredStaffCount

        private int CalculateRequiredStaffCount(Venue venue)
        {
            int totalSeats = venue.SeatCapacities.Sum(c => c.Total_Seats);
            // Standard rule: 1 staff per 100 seats, minimum 1 staff
            return Math.Max(1, (int)Math.Ceiling(totalSeats / 100.0));
        }

        #endregion

        #region GetEventsByInterestedRegionsAsync

        public async Task<System.Collections.Generic.IEnumerable<BrowsedEventResponse>> GetEventsByInterestedRegionsAsync(int userId)
        {
            var user = await _userRepository.GetUserProfileAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            var regionIds = user.InterestedRegions.Select(r => r.Region_Id).ToList();
            if (!regionIds.Any())
            {
                return new List<BrowsedEventResponse>();
            }

            var rawEvents = await _eventRepository.GetEventsByRegionsAsync(regionIds);
            var response = new List<BrowsedEventResponse>();
            foreach (var ev in rawEvents)
            {
                var ticketTiers = new List<TicketTierDetailsDto>();
                if (ev.TicketTiers != null)
                {
                    foreach (var tier in ev.TicketTiers)
                    {
                        ticketTiers.Add(new TicketTierDetailsDto
                        {
                            Tier_Name = tier.Tier_Name,
                            Price = tier.Price,
                            Tickets_Sold = tier.Tickets_Sold
                        });
                    }
                }

                response.Add(new BrowsedEventResponse
                {
                    Event_Id = ev.Event_Id,
                    Organizer_Name = ev.Organizer?.Name ?? string.Empty,
                    Organizer_Email = ev.Organizer?.Email,
                    Venue_Name = ev.Venue?.Name,
                    Address = ev.Venue?.Address,
                    Venue_Region_Name = ev.Venue?.Region?.Region_Name,
                    Event_Type = ev.Event_Type,
                    Title = ev.Title,
                    Description_Url = ev.Description_Url,
                    Image_Url = ev.Image_Url,
                    Date_Time = ev.Date_Time,
                    Status = ev.Status,
                    Duration_Hours = ev.Duration_Hours,
                    TicketTiers = ticketTiers
                });
            }

            return response;
        }

        #endregion

        #region GetPopularRegionsAsync

        public async Task<IEnumerable<RegionResponse>> GetPopularRegionsAsync(int? rankNumber)
        {
            var regions = await _eventRepository.GetPopularRegionsAsync(rankNumber);
            return regions.Select(r => new RegionResponse
            {
                Region_Id = r.Region_Id,
                Region_Name = r.Region_Name,
                No_Of_Staffs = r.No_Of_Staffs
            });
        }

        #endregion

        #region GetTrendingEventsAsync

        public async Task<IEnumerable<BrowsedEventResponse>> GetTrendingEventsAsync(int? count)
        {
            var rawEvents = await _eventRepository.GetTrendingEventsAsync(count);
            var response = new List<BrowsedEventResponse>();
            foreach (var ev in rawEvents)
            {
                var ticketTiers = new List<TicketTierDetailsDto>();
                if (ev.TicketTiers != null)
                {
                    foreach (var tier in ev.TicketTiers)
                    {
                        ticketTiers.Add(new TicketTierDetailsDto
                        {
                            Tier_Name = tier.Tier_Name,
                            Price = tier.Price,
                            Tickets_Sold = tier.Tickets_Sold
                        });
                    }
                }

                response.Add(new BrowsedEventResponse
                {
                    Event_Id = ev.Event_Id,
                    Organizer_Name = ev.Organizer?.Name ?? string.Empty,
                    Organizer_Email = ev.Organizer?.Email,
                    Venue_Name = ev.Venue?.Name,
                    Address = ev.Venue?.Address,
                    Venue_Region_Name = ev.Venue?.Region?.Region_Name,
                    Event_Type = ev.Event_Type,
                    Title = ev.Title,
                    Description_Url = ev.Description_Url,
                    Image_Url = ev.Image_Url,
                    Date_Time = ev.Date_Time,
                    Status = ev.Status,
                    Duration_Hours = ev.Duration_Hours,
                    TicketTiers = ticketTiers
                });
            }

            return response;
        }

        #endregion

        #region GetPopularEventsInCommonAsync

        public async Task<IEnumerable<BrowsedEventResponse>> GetPopularEventsInCommonAsync(int regionsLimit)
        {
            var rawEvents = await _eventRepository.GetPopularEventsInCommonAsync(regionsLimit);
            var response = new List<BrowsedEventResponse>();
            foreach (var ev in rawEvents)
            {
                var ticketTiers = new List<TicketTierDetailsDto>();
                if (ev.TicketTiers != null)
                {
                    foreach (var tier in ev.TicketTiers)
                    {
                        ticketTiers.Add(new TicketTierDetailsDto
                        {
                            Tier_Name = tier.Tier_Name,
                            Price = tier.Price,
                            Tickets_Sold = tier.Tickets_Sold
                        });
                    }
                }

                response.Add(new BrowsedEventResponse
                {
                    Event_Id = ev.Event_Id,
                    Organizer_Name = ev.Organizer?.Name ?? string.Empty,
                    Organizer_Email = ev.Organizer?.Email,
                    Venue_Name = ev.Venue?.Name,
                    Address = ev.Venue?.Address,
                    Venue_Region_Name = ev.Venue?.Region?.Region_Name,
                    Event_Type = ev.Event_Type,
                    Title = ev.Title,
                    Description_Url = ev.Description_Url,
                    Image_Url = ev.Image_Url,
                    Date_Time = ev.Date_Time,
                    Status = ev.Status,
                    Duration_Hours = ev.Duration_Hours,
                    TicketTiers = ticketTiers
                });
            }

            return response;
        }

        #endregion

        #region GetEventTicketTierCapacitiesAsync

        public async Task<IEnumerable<TicketTierCapacityResponse>> GetEventTicketTierCapacitiesAsync(int eventId)
        {
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);
            if (ev == null)
            {
                throw new NotFoundException("Event not found.");
            }

            if (ev.Status != "Live")
            {
                throw new NotFoundException("Event not found or is no longer available.");
            }

            var response = new List<TicketTierCapacityResponse>();
            bool isVirtual = ev.Event_Type.Equals("Virtual", StringComparison.OrdinalIgnoreCase);

            foreach (var t in ev.TicketTiers)
            {
                int totalSeats = -1;
                int availableSeats = -1;

                if (!isVirtual && ev.Venue != null)
                {
                    var capacity = ev.Venue.SeatCapacities.FirstOrDefault(c => c.Tier_Name.Equals(t.Tier_Name, StringComparison.OrdinalIgnoreCase));
                    if (capacity != null)
                    {
                        totalSeats = capacity.Total_Seats;
                        availableSeats = Math.Max(0, totalSeats - t.Tickets_Sold);
                    }
                    else
                    {
                        totalSeats = 0;
                        availableSeats = 0;
                    }
                }

                response.Add(new TicketTierCapacityResponse
                {
                    Tier_Name = t.Tier_Name,
                    Total_Seats = totalSeats,
                    Available_Seats = availableSeats,
                    Tickets_Sold = t.Tickets_Sold
                });
            }

            return response;
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
                    var data = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(jsonContent);
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

        #region GetPlatformSettingsAsync

        public async Task<Event.Models.DTOs.PlatformSettingsResponse?> GetPlatformSettingsAsync()
        {
            var settings = await _settingsRepository.GetSettingsAsync();
            if (settings == null) return null;

            return new Event.Models.DTOs.PlatformSettingsResponse
            {
                Staff_Flat_Rate = settings.Staff_Flat_Rate,
                Virtual_Event_Activation_Fee = settings.Virtual_Event_Activation_Fee,
                Physical_Event_Activation_Fee = settings.Physical_Event_Activation_Fee,
                Ticket_Commission_Percentage = settings.Ticket_Commission_Percentage,
                Ticket_Fixed_Fee = settings.Ticket_Fixed_Fee,
                Max_Tickets_Per_Booking = settings.Max_Tickets_Per_Booking,
                GST_Percentage = settings.GST_Percentage
            };
        }

        #endregion

        #region FileStorage

        private string GetBaseAssetsPath()
        {
            var currentDir = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return currentDir.EndsWith("Event.API") 
                ? Path.GetFullPath(Path.Combine(currentDir, "..", "Event.Business", "assets")) 
                : Path.GetFullPath(Path.Combine(currentDir, "Event.Business", "assets"));
        }

        public async Task<string> SaveDescriptionFileAsync(string text)
        {
            var tempId = Guid.NewGuid().ToString("N");
            var assetsDir = Path.Combine(GetBaseAssetsPath(), "events", "temp", tempId);
            Directory.CreateDirectory(assetsDir);
            var filePath = Path.Combine(assetsDir, "description.txt");
            await File.WriteAllTextAsync(filePath, text);
            return $"assets/events/temp/{tempId}/description.txt";
        }

        public async Task<string> SaveImageFileAsync(string fileName, byte[] fileBytes)
        {
            var tempId = Guid.NewGuid().ToString("N");
            var assetsDir = Path.Combine(GetBaseAssetsPath(), "events", "temp", tempId);
            Directory.CreateDirectory(assetsDir);
            var ext = Path.GetExtension(fileName);
            var filePath = Path.Combine(assetsDir, $"image{ext}");
            await File.WriteAllBytesAsync(filePath, fileBytes);
            return $"assets/events/temp/{tempId}/image{ext}";
        }

        #endregion
        #region UpdateEventDetailsAsync

        public async Task<bool> UpdateEventDetailsAsync(int organizerId, int eventId, UpdateEventDetailsRequest request)
        {
            var ev = await _eventRepository.GetByIdAsync(eventId);
            if (ev == null)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            if (ev.Organizer_Id != organizerId)
                throw new ValidationException("You are not authorized to update this event.");

            // Update Description without limitation
            if (request.Description_Url != null)
            {
                ev.Description_Url = request.Description_Url;
            }
            
            if (request.DescriptionText != null)
            {
                var assetsDir = Path.Combine(GetBaseAssetsPath(), "events", eventId.ToString());
                Directory.CreateDirectory(assetsDir);
                var filePath = Path.Combine(assetsDir, "description.txt");
                await File.WriteAllTextAsync(filePath, request.DescriptionText);
                ev.Description_Url = $"assets/events/{eventId}/description.txt";
            }

            // Restrict Title updates to a max of 2
            if (request.Title != null && request.Title != ev.Title)
            {
                if (ev.Title_Update_Count >= 2)
                {
                    throw new ValidationException("Event title can only be updated a maximum of 2 times.");
                }
                
                ev.Title = request.Title;
                ev.Title_Update_Count++;
            }

            await _eventRepository.UpdateAsync(ev);
            return true;
        }

        #endregion
    }
}
