using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class OrganizerUpfrontPaymentRepository : GenericRepository<OrganizerUpfrontPayment>, IOrganizerUpfrontPaymentRepository
    {
        public OrganizerUpfrontPaymentRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<OrganizerUpfrontPayment>> GetUpfrontPaymentsByEventIdAsync(int eventId)
        {
            return await _dbSet
                .Include(oup => oup.Transaction)
                .Where(oup => oup.Event_Id == eventId)
                .ToListAsync();
        }
    }
}
