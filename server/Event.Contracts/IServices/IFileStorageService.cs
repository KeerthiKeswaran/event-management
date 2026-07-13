using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IFileStorageService
    {
        Task<string> SaveTextAsync(string relativeAssetPath, string content);
        Task<string> SaveBytesAsync(string relativeAssetPath, byte[] bytes);
        string GetUrl(string relativeAssetPath);
        Task<string> ReadTextAsync(string relativeAssetPath);
    }
}
