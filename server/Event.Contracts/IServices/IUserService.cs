using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IUserService
    {
        int GetCurrentUserId();
        Task<bool> SelectInterestedRegionsAsync(int userId, string regionId);
        Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileRequest request);
        Task<UserProfileResponse?> GetUserProfileAsync(int userId);
        Task<IEnumerable<MyEventOverviewResponse>> GetMyEventsAsync(int organizerId);
        Task<MyEventDetailsResponse?> GetMyEventDetailsAsync(int organizerId, int eventId);
        Task<bool> CloseAccountAsync(int userId, CloseAccountRequest request);
    }
}
