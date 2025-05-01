using System.IO;
using System.Threading.Tasks;

namespace web_backend.Services
{
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a stream to Azure Blob Storage
        /// </summary>
        /// <param name="content">The stream content to upload</param>
        /// <param name="fileName">The name of the file</param>
        /// <param name="contentType">The content type (MIME type) of the file</param>
        /// <param name="containerName">The container to upload to</param>
        /// <returns>The URL of the uploaded file</returns>
        Task<string> UploadStreamAsync(Stream content, string fileName, string contentType, string containerName);
        
        /// <summary>
        /// Deletes a file from Azure Blob Storage
        /// </summary>
        /// <param name="blobUrl">The URL of the blob to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteFileAsync(string blobUrl);
    }
}