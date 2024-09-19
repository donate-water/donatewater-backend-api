using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Azure.Storage.Blobs;
using IIASA.FieldSurvey.Config;
using IIASA.FieldSurvey.services;

namespace IIASA.FieldSurvey.core;

public class AzureFileUploader : IAzureFileUploader
{
    private readonly AzureConfig _azureConfig;

    public AzureFileUploader(AzureConfig azureConfig)
    {
        _azureConfig = azureConfig;
    }

    public async Task<string> UploadFileAsync(string folderName, Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var containerClient = new BlobContainerClient(_azureConfig.StorageConnectionStrings, _azureConfig.ContainerName);
        var blobName = $"{folderName}/{Guid.NewGuid():N}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, true);
        return HttpUtility.UrlDecode(blobClient.Uri.AbsoluteUri);
    }
}