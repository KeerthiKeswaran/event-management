using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class TermsAndConditionsRepository : GenericRepository<TermsAndConditions>, ITermsAndConditionsRepository
    {
        public TermsAndConditionsRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<TermsAndConditions?> GetActiveTermsAsync()
        {
            return await _dbSet.FirstOrDefaultAsync(tc => tc.Is_Active && tc.Type == "General");
        }

        public async Task<TermsAndConditions?> GetTermsByVersionAsync(string version)
        {
            return await _dbSet.FirstOrDefaultAsync(tc => tc.Version == version);
        }

        public async Task<TermsAndConditions?> GetActiveTermsByTypeAsync(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            return await _dbSet.FirstOrDefaultAsync(tc => tc.Is_Active && tc.Type.ToLower() == type.ToLower());
        }
    }
}
