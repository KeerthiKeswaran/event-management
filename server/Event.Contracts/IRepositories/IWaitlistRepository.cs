using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IWaitlistRepository : IGenericRepository<Waitlist>
    {
        Task<Waitlist?> GetNextEligibleAsync(int eventId, string tierName, int maxQty);
        Task<IEnumerable<Waitlist>> GetWaitlistByUserAndEventAsync(int userId, int eventId);
        Task<IEnumerable<Waitlist>> GetMyActiveWaitlistsAsync(int userId);
        Task<int> GetWaitlistPositionAsync(int waitlistId);
        Task<IEnumerable<Waitlist>> GetExpiredNotifiedEntriesAsync(DateTime cutoffTime);
        Task<IEnumerable<Waitlist>> GetWaitlistByEventAsync(int eventId);
        Task<IEnumerable<Waitlist>> GetWaitingQueueAsync(int eventId, string tierName);
        Task<IEnumerable<Waitlist>> GetWaitingQueueAfterPositionAsync(int eventId, string tierName, int position);
        Task<int> GetNextPositionAsync(int eventId, string tierName);
        Task<bool> HasActiveWaitlistAsync(int eventId, string tierName);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
