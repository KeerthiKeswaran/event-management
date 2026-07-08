using System.Threading.Tasks;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IPolicyService
    {
        Task<PolicyResponse?> GetPolicyByTypeAsync(string type);
    }
}
