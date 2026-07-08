using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class EventRepository : GenericRepository<Event.Models.Event>, IEventRepository
    {
        public EventRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<Event.Models.Event>> SearchEventsAsync(
            string? keyword, 
            string? category, 
            DateTime? minDateTime, 
            string? regionId, 
            string? format, 
            decimal? maxPrice, 
            string? sortBy, 
            int page, 
            int size)
        {
            var query = _dbSet.Where(e => e.Status == "Live");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(e => 
                    e.Title.ToLower().Contains(kw) || 
                    e.Category.ToLower().Contains(kw) || 
                    e.Event_Type.ToLower().Contains(kw) || 
                    (e.Venue != null && e.Venue.Name.ToLower().Contains(kw)) ||
                    (e.Venue != null && e.Venue.Address.ToLower().Contains(kw)) ||
                    (e.Venue != null && e.Venue.Region != null && e.Venue.Region.Region_Name.ToLower().Contains(kw))
                );
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var lowerCategory = category.ToLower();
                query = query.Where(e => e.Category.ToLower() == lowerCategory);
            }

            if (minDateTime.HasValue)
            {
                query = query.Where(e => e.Date_Time >= minDateTime.Value);
            }

            if (!string.IsNullOrWhiteSpace(regionId))
            {
                query = query.Where(e => e.Venue != null && e.Venue.Region_Id == regionId);
            }

            if (!string.IsNullOrWhiteSpace(format))
            {
                var lowerFormat = format.ToLower();
                query = query.Where(e => e.Event_Type.ToLower() == lowerFormat);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(e => e.TicketTiers != null && e.TicketTiers.Any(t => t.Price <= maxPrice.Value));
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var sort = sortBy.ToLower();
                if (sort == "price-low")
                {
                    query = query.OrderBy(e => e.TicketTiers != null ? e.TicketTiers.Min(t => t.Price) : 0);
                }
                else if (sort == "price-high")
                {
                    query = query.OrderByDescending(e => e.TicketTiers != null ? e.TicketTiers.Min(t => t.Price) : 0);
                }
                else
                {
                    query = query.OrderByDescending(e => e.Event_Id);
                }
            }
            else
            {
                query = query.OrderByDescending(e => e.Event_Id);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                    .ThenInclude(v => v.Region)
                .Include(e => e.TicketTiers)
                .Include(e => e.Reports)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PagedResult<Event.Models.Event>(items, totalCount, page, size);
        }

        public async Task<Event.Models.Event?> GetEventDetailsAsync(int eventId)
        {
            return await _dbSet
                .Include(e => e.Venue)
                    .ThenInclude(v => v.SeatCapacities)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTiers)
                .Include(e => e.StaffAllocations)
                .FirstOrDefaultAsync(e => e.Event_Id == eventId);
        }

        public async Task<bool> ExistsAsync(int eventId)
        {
            return await _dbSet.AnyAsync(e => e.Event_Id == eventId);
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetExpiredEventsAsync(DateTime cutoffTime)
        {
            // Join with transaction or check creation time (since Event does not have Created_At, we can look up the pending transaction creation time or assume they expire if they are past the event start time or pending longer than cutoffTime). 
            // In our case, the Transaction table has Created_At and Related_Id. We can query events that are "Activation Pending" and have a transaction older than cutoffTime.
            return await _dbSet
                .Where(e => e.Status == "Activation Pending" && 
                            _context.Transactions.Any(t => t.Related_Id == e.Event_Id && 
                                                           t.Transaction_Type == "OrganizerUpfrontPayment" && 
                                                           t.Status == "Pending" && 
                                                           t.Created_At <= cutoffTime))
                .ToListAsync();
        }

        public async Task AddReportAsync(EventReport report)
        {
            await _context.EventReports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<EventReport>> GetAllReportsAsync()
        {
            return await _context.EventReports
                .Include(er => er.Event)
                .Include(er => er.Reporter)
                .ToListAsync();
        }

        public async Task<EventReport?> GetReportByIdAsync(int reportId)
        {
            return await _context.EventReports
                .Include(er => er.Event)
                    .ThenInclude(e => e.Organizer)
                .Include(er => er.Reporter)
                .FirstOrDefaultAsync(er => er.Report_Id == reportId);
        }

        public async Task UpdateReportAsync(EventReport report)
        {
            _context.EventReports.Update(report);
            await _context.SaveChangesAsync();
        }

        public async Task AddFeedbackAsync(EventFeedback feedback)
        {
            await _context.EventFeedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByRegionsAsync(System.Collections.Generic.IEnumerable<string> regionIds)
        {
            return await _dbSet
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                    .ThenInclude(v => v.Region)
                .Include(e => e.TicketTiers)
                .Include(e => e.Reports)
                .Where(e => e.Status == "Live" && e.Venue != null && regionIds.Contains(e.Venue.Region_Id))
                .ToListAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetLiveEventsWithDetailsAsync()
        {
            return await _dbSet
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.StaffAllocations)
                .Where(e => e.Status == "Live")
                .ToListAsync();
        }

        public async Task<PagedResult<Event.Models.Event>> GetEventsPagedAsync(
            string? keyword,
            string? eventType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size)
        {
            var query = _dbSet
                .Include(e => e.Venue)
                    .ThenInclude(v => v.SeatCapacities)
                .Include(e => e.Organizer)
                .Include(e => e.StaffAllocations)
                .AsQueryable();

            // Filter by keyword (title or description)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(lowerKeyword) || e.Description_Url.ToLower().Contains(lowerKeyword));
            }

            // Filter by event type
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                var lowerType = eventType.ToLower();
                query = query.Where(e => e.Event_Type.ToLower() == lowerType);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                var lowerStatus = status.ToLower();
                query = query.Where(e => e.Status.ToLower() == lowerStatus);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                var utcStart = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(e => e.Date_Time >= utcStart);
            }
            if (endDate.HasValue)
            {
                var utcEnd = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                query = query.Where(e => e.Date_Time <= utcEnd);
            }

            // Sort
            if (string.Equals(sortBy, "date_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(e => e.Date_Time);
            }
            else if (string.Equals(sortBy, "title_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(e => e.Title);
            }
            else if (string.Equals(sortBy, "title_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(e => e.Title);
            }
            else if (string.Equals(sortBy, "status_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(e => e.Status);
            }
            else if (string.Equals(sortBy, "status_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(e => e.Status);
            }
            else
            {
                // Default: earliest first (date_asc)
                query = query.OrderBy(e => e.Date_Time);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PagedResult<Event.Models.Event>(items, totalCount, page, size);
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByOrganizerAsync(int organizerId)
        {
            return await _dbSet
                .Include(e => e.Venue)
                .Include(e => e.TicketTiers)
                .Where(e => e.Organizer_Id == organizerId)
                .ToListAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<Region>> GetPopularRegionsAsync(int? limit)
        {
            var query = _context.Regions
                .Select(r => new
                {
                    Region = r,
                    EventCount = r.Venues.SelectMany(v => v.Events).Count(e => e.Status == "Live")
                })
                .OrderByDescending(x => x.EventCount)
                .ThenBy(x => x.Region.Region_Name)
                .Select(x => x.Region);

            if (limit.HasValue && limit.Value > 0)
            {
                return await query.Take(limit.Value).ToListAsync();
            }
            return await query.ToListAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetTrendingEventsAsync(int? limit)
        {
            var query = _dbSet
                .Include(e => e.Venue)
                    .ThenInclude(v => v.Region)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTiers)
                .Include(e => e.Reports)
                .Where(e => e.Status == "Live")
                .OrderByDescending(e => e.Bookings.Count)
                .ThenByDescending(e => e.Date_Time);

            if (limit.HasValue && limit.Value > 0)
            {
                return await query.Take(limit.Value).ToListAsync();
            }
            return await query.ToListAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetPopularEventsInCommonAsync(int regionsLimit)
        {
            var popularRegionIds = await _context.Regions
                .Select(r => new
                {
                    RegionId = r.Region_Id,
                    EventCount = r.Venues.SelectMany(v => v.Events).Count(e => e.Status == "Live")
                })
                .OrderByDescending(x => x.EventCount)
                .ThenBy(x => x.RegionId)
                .Select(x => x.RegionId)
                .Take(regionsLimit)
                .ToListAsync();

            return await _dbSet
                .Include(e => e.Venue)
                    .ThenInclude(v => v.Region)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTiers)
                .Include(e => e.Reports)
                .Where(e => e.Status == "Live" && popularRegionIds.Contains(e.Venue.Region_Id))
                .OrderByDescending(e => e.Bookings.Count)
                .ThenByDescending(e => e.Date_Time)
                .ToListAsync();
        }
    
        public async Task<PagedResult<Event.Models.Event>> GetEventsForPayoutsAsync(string? status, string? sortBy, int page, int size)
        {
            var query = _dbSet
                .Include(e => e.Organizer)
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.Payments)
                .Where(e => e.Status == "Live" || e.Status == "Completed")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Upcoming", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(e => e.Status == "Live");
                else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(e => e.Status == "Completed");
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var parts = sortBy.Split('_');
                var column = parts[0].ToLower();
                var direction = parts.Length > 1 ? parts[1].ToLower() : "asc";

                if (column == "status")
                {
                    if (direction == "asc")
                        query = query.OrderBy(e => e.Status);
                    else
                        query = query.OrderByDescending(e => e.Status);
                }
                else if (column == "date")
                {
                    if (direction == "asc")
                        query = query.OrderBy(e => e.Date_Time);
                    else
                        query = query.OrderByDescending(e => e.Date_Time);
                }
                else
                {
                    query = query.OrderByDescending(e => e.Date_Time);
                }
            }
            else
            {
                query = query.OrderByDescending(e => e.Date_Time);
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PagedResult<Event.Models.Event>(items, totalCount, page, size);
        }


        public async Task<bool> HasUserReportedEventAsync(int eventId, int userId)
        {
            return false;
        }

        public async Task<System.Collections.Generic.IEnumerable<int>> GetReportedEventIdsAsync(int userId)
        {
            return new List<int>();
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.EventFeedback>> GetFeedbacksByAttendeeAsync(int attendeeId)
        {
            return new List<Event.Models.EventFeedback>();
        }
}
}
