using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using SmartSpend.Core;

namespace SmartSpend.Services
{
    [ExcludeFromCodeCoverage]
    public class AzureStorageService : IStorageService
    {
        public AzureStorageService(string connection)
        {
            _connection = connection;
        }

        public string ContainerName { get; set; }

        public async Task<string> DownloadBlobAsync(string filename, Stream stream)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            if (!await containerClient.ExistsAsync())
                throw new DirectoryNotFoundException($"No such container {ContainerName}");

            var blob = containerClient.GetBlobClient(filename);
            if (!await blob.ExistsAsync())
                throw new FileNotFoundException($"No such file {filename}");

            await blob.DownloadToAsync(stream);

            var props = await blob.GetPropertiesAsync();
            return props.Value.ContentType;
        }

        public async Task<Uri> UploadBlobAsync(string filename, Stream stream, string contenttype)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(filename);

            var options = new BlobUploadOptions()
            {
                HttpHeaders = new BlobHttpHeaders() { ContentType = contenttype }
            };

            await blobClient.UploadAsync(stream, options);
            return blobClient.Uri;
        }

        public async Task<IEnumerable<string>> GetBlobNamesAsync(string prefix = null)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobs = containerClient.GetBlobsAsync(prefix:prefix);

            var result = new List<string>();
            await foreach(var blob in blobs)
                result.Add(blob.Name);

            return result;
        }
        public async Task RemoveBlobAsync(string filename)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connection);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            await containerClient.DeleteBlobIfExistsAsync(filename);
        }

        private readonly string _connection;
    }
}
