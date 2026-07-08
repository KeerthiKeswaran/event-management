using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IOrganizerPayoutRepository : IGenericRepository<OrganizerPayout>
    {
        Task<OrganizerPayout?> GetPayoutByEventIdAsync(int eventId);
        Task<IEnumerable<OrganizerPayout>> GetPendingPayoutsAsync();
    }
}
