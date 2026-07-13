using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Event.Business.Services
{
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["Storage:ConnectionString"] 
                ?? throw new InvalidOperationException("Blob connection string not found.");
            
            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient("assets");
            
            // In a real scenario, you'd ensure it exists here or via Bicep
            // _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> SaveTextAsync(string relativeAssetPath, string content)
        {
            var blobClient = _containerClient.GetBlobClient(relativeAssetPath);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "application/json" });
            return blobClient.Uri.ToString();
        }

        public async Task<string> SaveBytesAsync(string relativeAssetPath, byte[] bytes)
        {
            var blobClient = _containerClient.GetBlobClient(relativeAssetPath);
            using var stream = new MemoryStream(bytes);
            
            string ext = Path.GetExtension(relativeAssetPath).ToLower();
            string contentType = ext switch {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
            return blobClient.Uri.ToString();
        }

        public string GetUrl(string relativeAssetPath)
        {
            var blobClient = _containerClient.GetBlobClient(relativeAssetPath);
            return blobClient.Uri.ToString();
        }

        public async Task<string> ReadTextAsync(string relativeAssetPath)
        {
            var blobClient = _containerClient.GetBlobClient(relativeAssetPath);
            if (!await blobClient.ExistsAsync()) return string.Empty;

            var downloadResult = await blobClient.DownloadContentAsync();
            return downloadResult.Value.Content.ToString();
        }
    }
}
