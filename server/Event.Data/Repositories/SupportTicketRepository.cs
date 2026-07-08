using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class SupportTicketRepository : GenericRepository<SupportTicket>, ISupportTicketRepository
    {
        public SupportTicketRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SupportTicket>> GetTicketsByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(st => st.User_Id == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupportTicket>> GetPendingTicketsAsync()
        {
            return await _dbSet
                .Where(st => st.Status == "Open")
                .ToListAsync();
        }

        public async Task<IEnumerable<SupportTicket>> GetAllWithUsersAsync()
        {
            return await _dbSet
                .Include(st => st.User)
                .ToListAsync();
        }
    }
}
