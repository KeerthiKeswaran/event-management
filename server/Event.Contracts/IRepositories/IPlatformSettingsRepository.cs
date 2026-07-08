using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IPlatformSettingsRepository : IGenericRepository<PlatformSettings>
    {
        Task<PlatformSettings?> GetSettingsAsync();
    }
}
