using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IRegionRepository : IGenericRepository<Region>
    {
        Task<Region?> GetByRegionIdAsync(string regionId);
    }
}
