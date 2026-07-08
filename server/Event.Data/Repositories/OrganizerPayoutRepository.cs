using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class OrganizerPayoutRepository : GenericRepository<OrganizerPayout>, IOrganizerPayoutRepository
    {
        public OrganizerPayoutRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<OrganizerPayout?> GetPayoutByEventIdAsync(int eventId)
        {
            return await _dbSet
                .Include(op => op.Transaction)
                .FirstOrDefaultAsync(op => op.Event_Id == eventId);
        }

        public async Task<IEnumerable<OrganizerPayout>> GetPendingPayoutsAsync()
        {
            return await _dbSet
                .Where(op => op.Payout_Status != "Success")
                .ToListAsync();
        }
    }
}
