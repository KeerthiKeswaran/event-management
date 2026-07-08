using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface ISupportService
    {
        Task<bool> SubmitSupportTicketAsync(int userId, string subject, string message, string requestType, int? relatedId = null, string? targetType = null);
        Task<System.Collections.Generic.IEnumerable<Event.Models.DTOs.SupportTicketDto>> GetMySupportTicketsAsync(int userId);
    }
}
