using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class AdminRepository : GenericRepository<Admin>, IAdminRepository
    {
        public AdminRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<Admin?> GetByAdminIdAsync(string adminId)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Admin_Id == adminId);
        }

        public async Task<Admin?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Email == email);
        }
    }
}
