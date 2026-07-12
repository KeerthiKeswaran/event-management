using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IWaitlistService
    {
        Task<WaitlistStatusResponse> JoinWaitlistAsync(int userId, int eventId, string tierName, int quantity);
        Task ProcessWaitlistForEventTierAsync(int eventId, string tierName, int freedSeats);
        Task ExpireStaleWaitlistAsync();
        Task<bool> CancelWaitlistEntryAsync(int waitlistId, int userId);
        Task<IEnumerable<WaitlistStatusResponse>> GetMyWaitlistAsync(int userId);
        Task<IEnumerable<WaitlistStatusResponse>> GetWaitlistByEventAsync(int eventId);
    }
}
