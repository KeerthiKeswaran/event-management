using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Exceptions;
using Serilog;
using Event.Business.Helpers;

namespace Event.Business.Services
{
    public class WaitlistService : IWaitlistService
    {
        private readonly IWaitlistRepository _waitlistRepository;
        private readonly IEventRepository _eventRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly IBookingService _bookingService;

        public WaitlistService(
            IWaitlistRepository waitlistRepository,
            IEventRepository eventRepository,
            INotificationRepository notificationRepository,
            IEmailService emailService,
            IBookingService bookingService)
        {
            _waitlistRepository = waitlistRepository;
            _eventRepository = eventRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _bookingService = bookingService;
        }

        public async Task<WaitlistStatusResponse> JoinWaitlistAsync(int userId, int eventId, string tierName, int quantity)
        {
            await _waitlistRepository.BeginTransactionAsync();
            try
            {
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                if (ev.Status != "Live")
                    throw new ValidationException("Cannot join waitlist for non-live events.");

                var eventTier = ev.TicketTiers.FirstOrDefault(t => t.Tier_Name.Equals(tierName, StringComparison.OrdinalIgnoreCase));
                if (eventTier == null)
                    throw new NotFoundException($"Ticket tier '{tierName}' not found for event.");

                var existingEntries = await _waitlistRepository.GetWaitlistByUserAndEventAsync(userId, eventId);
                if (existingEntries.Any(w => w.Tier_Name.Equals(tierName, StringComparison.OrdinalIgnoreCase) && (w.Status == "Waiting" || w.Status == "Notified")))
                {
                    throw new ConflictException("You are already on the waitlist for this tier.");
                }

                // Check if seats are actually available right now
                bool hasSeats = false;
                if (ev.Event_Type.Equals("Physical", StringComparison.OrdinalIgnoreCase) || ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    if (ev.Venue != null)
                    {
                        var capacity = ev.Venue.SeatCapacities.FirstOrDefault(c => c.Tier_Name.Equals(tierName, StringComparison.OrdinalIgnoreCase));
                        if (capacity != null)
                        {
                            int available = capacity.Total_Seats - eventTier.Tickets_Sold;
                            if (available >= quantity)
                            {
                                hasSeats = true;
                            }
                        }
                    }
                }
                else
                {
                    hasSeats = true; // Virtual has infinite seats? Assuming we waitlist virtual if they wanted to, but usually it's physical. 
                    // Actually, virtual might be unlimited. We'll follow the same logic.
                }

                bool hasActiveQueue = await _waitlistRepository.HasActiveWaitlistAsync(eventId, tierName);

                if (hasSeats && !hasActiveQueue)
                {
                    // Book immediately
                    var tierQuantities = new Dictionary<string, int> { { tierName, quantity } };
                    var bookingRes = await _bookingService.BookTicketsAsync(userId, eventId, tierQuantities);
                    
                    var entry = new Waitlist
                    {
                        Event_Id = eventId,
                        Attendee_Id = userId,
                        Tier_Name = tierName,
                        Quantity = quantity,
                        Status = "Booked",
                        Position = 0,
                        Joined_At = DateTime.UtcNow,
                        Booking_Id = bookingRes?.Booking_Id
                    };
                    await _waitlistRepository.AddAsync(entry);
                    await _waitlistRepository.CommitTransactionAsync();
                    return MapToResponse(entry, ev.Title);
                }
                else
                {
                    // Add to waitlist queue
                    int nextPos = await _waitlistRepository.GetNextPositionAsync(eventId, tierName);
                    var entry = new Waitlist
                    {
                        Event_Id = eventId,
                        Attendee_Id = userId,
                        Tier_Name = tierName,
                        Quantity = quantity,
                        Status = "Waiting",
                        Position = nextPos,
                        Joined_At = DateTime.UtcNow
                    };
                    await _waitlistRepository.AddAsync(entry);
                    await _waitlistRepository.CommitTransactionAsync();
                    return MapToResponse(entry, ev.Title);
                }
            }
            catch (Exception)
            {
                await _waitlistRepository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task ProcessWaitlistForEventTierAsync(int eventId, string tierName, int freedSeats)
        {
            var entry = await _waitlistRepository.GetNextEligibleAsync(eventId, tierName, freedSeats);
            if (entry == null) return;

            var ev = await _eventRepository.GetByIdAsync(eventId);
            if (ev == null) return;

            await _waitlistRepository.BeginTransactionAsync();
            try
            {
                entry.Status = "Notified";
                entry.Notified_At = DateTime.UtcNow;
                entry.Expires_At = CalculateExpiryDate(ev.Date_Time);

                await _waitlistRepository.UpdateAsync(entry);
                
                // Fetch user to get email
                var user = entry.Attendee;
                
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var emailDto = new EmailTemplateDto
                    {
                        TemplateName = "WaitlistNotificationTemplate.html",
                        Placeholders = new Dictionary<string, string>
                        {
                            { "eventName", ev.Title },
                            { "tierName", entry.Tier_Name },
                            { "quantity", entry.Quantity.ToString() },
                            { "expiryTime", entry.Expires_At.Value.ToString("MMM dd, yyyy HH:mm") },
                            { "bookingLink", $"http://localhost:4200/booking?eventId={ev.Event_Id}" }
                        }
                    };
                    string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);
                    await NotificationHelper.SendAndSaveNotificationAsync(
                        _notificationRepository,
                        _emailService,
                        user.Email,
                        $"Your Waitlist Slot is Ready: {ev.Title}",
                        htmlBody
                    );
                }

                await _waitlistRepository.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _waitlistRepository.RollbackTransactionAsync();
                Log.Error(ex, "Failed to process waitlist for event {EventId} tier {TierName}", eventId, tierName);
            }
        }

        public async Task ExpireStaleWaitlistAsync()
        {
            var logger = new LoggerConfiguration()
                .WriteTo.File("logs/business.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var now = DateTime.UtcNow;
            try
            {
                var expiredEntries = await _waitlistRepository.GetExpiredNotifiedEntriesAsync(now);
                foreach (var entry in expiredEntries)
                {
                    try
                    {
                        await _waitlistRepository.BeginTransactionAsync();
                        entry.Status = "Expired";
                        await _waitlistRepository.UpdateAsync(entry);
                        
                        // Shift remaining queue up by 1
                        var remaining = await _waitlistRepository.GetWaitingQueueAsync(entry.Event_Id, entry.Tier_Name);
                        
                        foreach (var w in remaining)
                        {
                            w.Position = Math.Max(1, w.Position - 1);
                            await _waitlistRepository.UpdateAsync(w);
                        }
                        
                        await _waitlistRepository.CommitTransactionAsync();

                        // Re-process waitlist for this tier with the freed quantity
                        await ProcessWaitlistForEventTierAsync(entry.Event_Id, entry.Tier_Name, entry.Quantity);
                    }
                    catch (Exception ex)
                    {
                        await _waitlistRepository.RollbackTransactionAsync();
                        logger.Error(ex, "Failed to expire waitlist entry {WaitlistId}", entry.Waitlist_Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in ExpireStaleWaitlistAsync");
            }
        }

        public async Task<bool> CancelWaitlistEntryAsync(int waitlistId, int userId)
        {
            await _waitlistRepository.BeginTransactionAsync();
            try
            {
                var entry = await _waitlistRepository.GetByIdAsync(waitlistId);
                if (entry == null)
                    throw new NotFoundException("Waitlist entry not found.");

                if (entry.Attendee_Id != userId)
                    throw new UnauthorizedAccessException("You can only cancel your own waitlist entries.");

                if (entry.Status != "Waiting" && entry.Status != "Notified")
                    throw new ValidationException("Only active waitlist entries can be cancelled.");

                string oldStatus = entry.Status;
                entry.Status = "Cancelled";
                await _waitlistRepository.UpdateAsync(entry);

                // Shift remaining queue up by 1
                var remaining = await _waitlistRepository.GetWaitingQueueAfterPositionAsync(entry.Event_Id, entry.Tier_Name, entry.Position);
                
                foreach (var w in remaining)
                {
                    w.Position = Math.Max(1, w.Position - 1);
                    await _waitlistRepository.UpdateAsync(w);
                }

                await _waitlistRepository.CommitTransactionAsync();

                if (oldStatus == "Notified")
                {
                    // They gave up their notified spot, trigger the next person
                    await ProcessWaitlistForEventTierAsync(entry.Event_Id, entry.Tier_Name, entry.Quantity);
                }

                return true;
            }
            catch (Exception)
            {
                await _waitlistRepository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<IEnumerable<WaitlistStatusResponse>> GetMyWaitlistAsync(int userId)
        {
            var entries = await _waitlistRepository.GetMyActiveWaitlistsAsync(userId);
            return entries.Select(e => MapToResponse(e, e.Event?.Title ?? string.Empty));
        }

        public async Task<IEnumerable<WaitlistStatusResponse>> GetWaitlistByEventAsync(int eventId)
        {
            var entries = await _waitlistRepository.GetWaitlistByEventAsync(eventId);
            return entries.Select(e => MapToResponse(e, e.Event?.Title ?? string.Empty));
        }

        private WaitlistStatusResponse MapToResponse(Waitlist entry, string eventTitle)
        {
            return new WaitlistStatusResponse
            {
                Waitlist_Id = entry.Waitlist_Id,
                Event_Id = entry.Event_Id,
                Event_Title = eventTitle,
                Tier_Name = entry.Tier_Name,
                Quantity = entry.Quantity,
                Status = entry.Status,
                Position = entry.Position,
                Joined_At = entry.Joined_At,
                Expires_At = entry.Expires_At
            };
        }

        private DateTime CalculateExpiryDate(DateTime eventDate)
        {
            var now = DateTime.UtcNow;
            var timeUntilEvent = eventDate - now;

            if (timeUntilEvent.TotalHours > 72)
            {
                return now.AddHours(24);
            }
            else if (timeUntilEvent.TotalHours > 24)
            {
                return now.AddHours(2);
            }
            else
            {
                return now.AddMinutes(30);
            }
        }
    }
}
