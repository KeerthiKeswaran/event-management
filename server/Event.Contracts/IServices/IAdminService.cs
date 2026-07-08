using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsDto> GetDashboardStatsAsync();
        Task<PagedResult<EventDetailEto>> GetEventsPagedAsync(string? keyword, string? eventType, string? status, DateTime? startDate, DateTime? endDate, string? sortBy, int page, int size);
        Task<EventDetailEto?> GetEventByIdAsync(int id);
        Task<object?> GetRelatedEntityAsync(string type, int id);
        Task<IEnumerable<SupportTicketResponse>> GetSupportTicketsAsync(string? status, string? keyword, DateTime? dateFrom, DateTime? dateTo);
        Task<bool> RespondToTicketAsync(int ticketId, string responseText);
        Task<bool> EscalateTicketAsync(int ticketId, string adminId, EscalateTicketRequest request);
        Task<AdminAction?> GetEscalationStatusAsync(int ticketId);
        Task<object> GetFlaggedEventsReportsAsync();
        Task<bool> DismissEventReportAsync(int reportId);
        Task<bool> UpholdEventReportAsync(int reportId, string adminId, string actionReason, string organizerAction);

        Task<IEnumerable<RegionResponse>> GetAllRegionsAsync();
        Task<IEnumerable<VenueResponse>> GetAllVenuesAsync();
        Task<VenueResponse> CreateVenueAsync(CreateVenueRequest request);
        Task<VenueResponse> UpdateVenueAsync(int venueId, CreateVenueRequest request);

        Task<PagedResult<StaffResponse>> GetStaffDirectoryAsync(string? regionId, bool? isAllocated, string? keyword, string? sortBy, int page = 1, int size = 10);
        Task<bool> AllocateStaffToEventAsync(int eventId, int employeeId);
        Task<IEnumerable<StaffResponse>> GetStaffByRegionAsync(string regionId);
        Task<IEnumerable<EventDetailEto>> GetEventsByRegionAsync(string regionId);

        Task<AdminProfileResponse> GetAdminProfileAsync(string adminId);
        Task<AdminProfileResponse> UpdateAdminProfileAsync(string adminId, UpdateAdminProfileRequest request);
        Task<HelpdeskMetadataResponse> GetHelpdeskMetadataAsync();
        Task<IEnumerable<VenueResponse>> GetAllVenuesIncludingInactiveAsync();
        Task<bool> UpdateEventVenueAsync(int eventId, int venueId);
        Task<object> SearchGlobalAsync(string keyword);
    }
}
