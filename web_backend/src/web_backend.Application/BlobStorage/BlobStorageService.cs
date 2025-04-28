using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace web_backend.BlobStorage
{
    public class BlobStorageService : ITransientDependency
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration config)
        {
            var containerName = config["BlobStorage:ContainerName"];
            var connectionString = config["BlobStorage:ConnectionString"];
            _containerClient = new BlobContainerClient(connectionString, containerName);
        }

        public BlobClient GetBlobClient(string blobName)
        {
            return _containerClient.GetBlobClient(blobName);
        }


        public async Task UploadAsync(string blobName, Stream fileStream)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(fileStream, overwrite: true);
        }

        public async Task<Stream> DownloadAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task DeleteAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<List<string>> ListBlobsAsync()
        {
            var results = new List<string>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                results.Add(blobItem.Name);
            }
            return results;
        }
    }

}
