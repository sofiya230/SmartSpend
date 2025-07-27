using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartSpend.Core;

public interface IStorageService
{
    Task<Uri> UploadBlobAsync(string filename, Stream stream, string contenttype);

    Task<string> DownloadBlobAsync(string filename, Stream stream);

    Task<IEnumerable<string>> GetBlobNamesAsync(string prefix = null);

    Task RemoveBlobAsync(string filename);

    public string ContainerName { get; set; }
}
