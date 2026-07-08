using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface ISupportTicketRepository : IGenericRepository<SupportTicket>
    {
        Task<IEnumerable<SupportTicket>> GetTicketsByUserIdAsync(int userId);
        Task<IEnumerable<SupportTicket>> GetPendingTicketsAsync();
        Task<IEnumerable<SupportTicket>> GetAllWithUsersAsync();
    }
}
