using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class PlatformSettingsRepository : GenericRepository<PlatformSettings>, IPlatformSettingsRepository
    {
        public PlatformSettingsRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<PlatformSettings?> GetSettingsAsync()
        {
            return await _dbSet.FirstOrDefaultAsync();
        }
    }
}
