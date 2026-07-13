using System.IO;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Microsoft.Extensions.Hosting;

namespace Event.Business.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _assetsRootPath;

        public LocalFileStorageService(IHostEnvironment env)
        {
            var currentDir = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            _assetsRootPath = currentDir.EndsWith("Event.API") 
                ? Path.GetFullPath(Path.Combine(currentDir, "..", "Event.Business", "assets")) 
                : Path.GetFullPath(Path.Combine(currentDir, "Event.Business", "assets"));
        }

        public async Task<string> SaveTextAsync(string relativeAssetPath, string content)
        {
            string fullPath = Path.Combine(_assetsRootPath, relativeAssetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, content);
            return GetUrl(relativeAssetPath);
        }

        public async Task<string> SaveBytesAsync(string relativeAssetPath, byte[] bytes)
        {
            string fullPath = Path.Combine(_assetsRootPath, relativeAssetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, bytes);
            return GetUrl(relativeAssetPath);
        }

        public string GetUrl(string relativeAssetPath)
        {
            return $"/assets/{relativeAssetPath.Replace("\\", "/")}";
        }

        public async Task<string> ReadTextAsync(string relativeAssetPath)
        {
            string fullPath = Path.Combine(_assetsRootPath, relativeAssetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (!File.Exists(fullPath)) return string.Empty;
            return await File.ReadAllTextAsync(fullPath);
        }
    }
}
