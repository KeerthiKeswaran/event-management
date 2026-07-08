using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsAsync(int userId);
        Task<User?> GetUserProfileAsync(int userId);
        Task UpdateInterestedRegionsAsync(int userId, IEnumerable<string> regionIds);
    }
}
