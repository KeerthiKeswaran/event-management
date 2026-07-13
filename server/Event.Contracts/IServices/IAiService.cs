using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IAiService
    {
        Task<string> GenerateEventDescriptionAsync(string keywords);
    }
}
