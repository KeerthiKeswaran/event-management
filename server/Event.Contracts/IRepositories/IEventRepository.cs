using System;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IEventRepository : IGenericRepository<Event.Models.Event>
    {
        Task<PagedResult<Event.Models.Event>> SearchEventsAsync(string? keyword, string? category, DateTime? minDateTime, string? regionId, string? format, decimal? maxPrice, string? sortBy, int page, int size);
        Task<Event.Models.Event?> GetEventDetailsAsync(int eventId);
        Task<bool> ExistsAsync(int eventId);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetExpiredEventsAsync(DateTime cutoffTime);
        Task AddReportAsync(EventReport report);
        Task<bool> HasUserReportedEventAsync(int eventId, int userId);
        Task<System.Collections.Generic.IEnumerable<int>> GetReportedEventIdsAsync(int userId);
        Task<System.Collections.Generic.IEnumerable<EventReport>> GetAllReportsAsync();
        Task<EventReport?> GetReportByIdAsync(int reportId);
        Task UpdateReportAsync(EventReport report);
        Task AddFeedbackAsync(EventFeedback feedback);
        Task<System.Collections.Generic.IEnumerable<EventFeedback>> GetFeedbacksByAttendeeAsync(int attendeeId);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByRegionsAsync(System.Collections.Generic.IEnumerable<string> regionIds);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetLiveEventsWithDetailsAsync();
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByOrganizerAsync(int organizerId);
        Task<PagedResult<Event.Models.Event>> GetEventsPagedAsync(string? keyword, string? eventType, string? status, DateTime? startDate, DateTime? endDate, string? sortBy, int page, int size);
        Task<System.Collections.Generic.IEnumerable<Region>> GetPopularRegionsAsync(int? limit);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetTrendingEventsAsync(int? limit);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetPopularEventsInCommonAsync(int regionsLimit);
        Task<PagedResult<Event.Models.Event>> GetEventsForPayoutsAsync(string? status, string? sortBy, int page, int size);
    }
}
