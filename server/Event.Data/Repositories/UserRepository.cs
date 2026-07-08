using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsAsync(int userId)
        {
            return await _dbSet.AnyAsync(u => u.User_Id == userId);
        }

        public async Task<User?> GetUserProfileAsync(int userId)
        {
            return await _dbSet
                .Include(u => u.InterestedRegions)
                .FirstOrDefaultAsync(u => u.User_Id == userId);
        }

        public async Task UpdateInterestedRegionsAsync(int userId, IEnumerable<string> regionIds)
        {
            var existingRegions = await _context.UserInterestedRegions
                .Where(r => r.User_Id == userId)
                .ToListAsync();

            _context.UserInterestedRegions.RemoveRange(existingRegions);

            foreach (var regionId in regionIds)
            {
                var regionExists = await _context.Regions.AnyAsync(r => r.Region_Id == regionId);
                if (regionExists)
                {
                    await _context.UserInterestedRegions.AddAsync(new UserInterestedRegion
                    {
                        User_Id = userId,
                        Region_Id = regionId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
