using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IAdminRepository : IGenericRepository<Admin>
    {
        Task<Admin?> GetByAdminIdAsync(string adminId);
        Task<Admin?> GetByEmailAsync(string email);
    }
}
