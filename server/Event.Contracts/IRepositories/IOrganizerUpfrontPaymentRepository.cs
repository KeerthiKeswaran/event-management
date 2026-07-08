using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IOrganizerUpfrontPaymentRepository : IGenericRepository<OrganizerUpfrontPayment>
    {
        Task<IEnumerable<OrganizerUpfrontPayment>> GetUpfrontPaymentsByEventIdAsync(int eventId);
    }
}
