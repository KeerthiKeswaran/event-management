using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int batchSize)
        {
            return await _dbSet
                .Where(n => n.Status == "Pending")
                .OrderBy(n => n.Created_At)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetFailedNotificationsForRetryAsync(int maxRetries, int batchSize)
        {
            return await _dbSet
                .Where(n => n.Status == "Failed" && n.Retry_Count < maxRetries)
                .OrderBy(n => n.Created_At)
                .Take(batchSize)
                .ToListAsync();
        }
    }
}
