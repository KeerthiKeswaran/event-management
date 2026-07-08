using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class RegionRepository : GenericRepository<Region>, IRegionRepository
    {
        public RegionRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<Region?> GetByRegionIdAsync(string regionId)
        {
            return await _context.Regions
                .FirstOrDefaultAsync(r => r.Region_Id == regionId);
        }
    }
}
