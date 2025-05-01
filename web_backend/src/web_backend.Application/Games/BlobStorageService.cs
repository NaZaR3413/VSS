using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;
using web_backend.Services;

namespace web_backend.Games
{
    public class BlobStorageService : IBlobStorageService, ITransientDependency
    {
        private readonly IConfiguration _configuration;
        private readonly string _storageConnectionString;
        private readonly string? _storageBaseUrl;

        public BlobStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
            _storageConnectionString = _configuration["Azure:BlobStorage:ConnectionString"] 
                ?? throw new ApplicationException("Azure Blob Storage connection string is missing");
            _storageBaseUrl = _configuration["Azure:BlobStorage:BaseUrl"];
            
            if (string.IsNullOrEmpty(_storageConnectionString))
            {
                throw new ApplicationException("Azure Blob Storage connection string is not configured!");
            }
        }

        public async Task<string> UploadStreamAsync(Stream content, string fileName, string contentType, string containerName)
        {
            if (content == null)
            {
                return string.Empty;
            }
            
            BlobContainerClient containerClient = new BlobContainerClient(_storageConnectionString, containerName.ToLower());
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            
            // Create a unique blob name using a timestamp and GUID
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string originalExtension = Path.GetExtension(fileName);
            string blobName = $"{Path.GetFileNameWithoutExtension(fileName).Replace(" ", "-")}-{timestamp}-{Guid.NewGuid()}{originalExtension}";
            
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            
            // Set position to beginning of stream
            content.Position = 0;
            
            // Upload file
            await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
            
            // Return full URL to the blob
            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteFileAsync(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl))
            {
                return false;
            }

            try
            {
                // Extract container name and blob name from URL
                Uri uri = new Uri(blobUrl);
                string path = uri.AbsolutePath;
                string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                if (segments.Length < 2)
                {
                    return false;
                }
                
                string containerName = segments[0];
                string blobName = string.Join("/", segments, 1, segments.Length - 1);
                
                // Create blob client and delete
                BlobContainerClient containerClient = new BlobContainerClient(_storageConnectionString, containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                
                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        private string GetContentType(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".mp4":
                    return "video/mp4";
                case ".webm":
                    return "video/webm";
                case ".mov":
                    return "video/quicktime";
                case ".avi":
                    return "video/x-msvideo";
                default:
                    return "application/octet-stream";
            }
        }
    }
}