using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface ITermsAndConditionsRepository : IGenericRepository<TermsAndConditions>
    {
        Task<TermsAndConditions?> GetActiveTermsAsync();
        Task<TermsAndConditions?> GetTermsByVersionAsync(string version);
        Task<TermsAndConditions?> GetActiveTermsByTypeAsync(string type);
    }
}
