using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int batchSize);
        Task<IEnumerable<Notification>> GetFailedNotificationsForRetryAsync(int maxRetries, int batchSize);
    }
}
