using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class WaitlistRepository : GenericRepository<Waitlist>, IWaitlistRepository
    {
        public WaitlistRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<Waitlist?> GetNextEligibleAsync(int eventId, string tierName, int maxQty)
        {
            return await _dbSet
                .Include(w => w.Attendee)
                .Where(w => w.Event_Id == eventId && w.Tier_Name == tierName && w.Status == "Waiting" && w.Quantity <= maxQty)
                .OrderBy(w => w.Position)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetWaitlistByUserAndEventAsync(int userId, int eventId)
        {
            return await _dbSet
                .Include(w => w.Event)
                .Where(w => w.Attendee_Id == userId && w.Event_Id == eventId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetMyActiveWaitlistsAsync(int userId)
        {
            return await _dbSet
                .Include(w => w.Event)
                .Where(w => w.Attendee_Id == userId && (w.Status == "Waiting" || w.Status == "Notified"))
                .ToListAsync();
        }

        public async Task<int> GetWaitlistPositionAsync(int waitlistId)
        {
            var entry = await _dbSet.FindAsync(waitlistId);
            return entry?.Position ?? 0;
        }

        public async Task<IEnumerable<Waitlist>> GetExpiredNotifiedEntriesAsync(DateTime cutoffTime)
        {
            return await _dbSet
                .Include(w => w.Event)
                .Include(w => w.Attendee)
                .Where(w => w.Status == "Notified" && w.Expires_At != null && w.Expires_At <= cutoffTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetWaitlistsForStartingEventsAsync(DateTime cutoffTime)
        {
            return await _dbSet
                .Include(w => w.Event)
                .Include(w => w.Attendee)
                .Where(w => (w.Status == "Waiting" || w.Status == "Notified") 
                    && w.Event != null 
                    && w.Event.Status == "Live" 
                    && w.Event.Date_Time <= cutoffTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetWaitlistByEventAsync(int eventId)
        {
            return await _dbSet
                .Include(w => w.Attendee)
                .Where(w => w.Event_Id == eventId)
                .OrderBy(w => w.Tier_Name)
                .ThenBy(w => w.Position)
                .ToListAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetWaitingQueueAsync(int eventId, string tierName)
        {
            return await _dbSet
                .Where(w => w.Event_Id == eventId && w.Tier_Name == tierName && w.Status == "Waiting")
                .ToListAsync();
        }

        public async Task<IEnumerable<Waitlist>> GetWaitingQueueAfterPositionAsync(int eventId, string tierName, int position)
        {
            return await _dbSet
                .Where(w => w.Event_Id == eventId && w.Tier_Name == tierName && w.Status == "Waiting" && w.Position > position)
                .ToListAsync();
        }

        public async Task<int> GetNextPositionAsync(int eventId, string tierName)
        {
            var maxPos = await _dbSet
                .Where(w => w.Event_Id == eventId && w.Tier_Name == tierName && w.Status == "Waiting")
                .MaxAsync(w => (int?)w.Position) ?? 0;
            return maxPos + 1;
        }

        public async Task<bool> HasActiveWaitlistAsync(int eventId, string tierName)
        {
            return await _dbSet
                .AnyAsync(w => w.Event_Id == eventId && w.Tier_Name == tierName && (w.Status == "Waiting" || w.Status == "Notified"));
        }

        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.CommitAsync();
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.RollbackAsync();
            }
        }
    }
}
