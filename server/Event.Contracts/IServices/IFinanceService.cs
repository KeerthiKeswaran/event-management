using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IServices
{
    public interface IFinanceService
    {
        Task<IEnumerable<object>> GetAdminActionsAsync();
        Task<bool> DeclineActionAsync(int actionId, string remarks);
        Task<bool> ApproveActionAsync(int actionId, string refundType, string refundMessage);
        Task<bool> RespondToTicketAsync(int ticketId, string responseText);
        Task<PagedResult<Transaction>> GetTransactionsPagedAsync(
            string? keyword,
            string? transactionType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size);
        Task<Event.Models.DTOs.FinanceDashboardStatsResponse> GetDashboardStatsAsync();
        Task<PagedResult<Event.Models.DTOs.OrganizerPayoutDto>> GetOrganizerPayoutsPagedAsync(string? status, string? sortBy, int page, int size);
    }
}
