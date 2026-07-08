using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class AdminActionRepository : GenericRepository<AdminAction>, IAdminActionRepository
    {
        public AdminActionRepository(EventDbContext context) : base(context)
        {
        }
    }
}
