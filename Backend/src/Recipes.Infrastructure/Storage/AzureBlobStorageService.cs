using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Recipes.Application.Common;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IOptions<BlobStorageOptions> options)
    {
        var opts = options.Value;
        var serviceClient = new BlobServiceClient(opts.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(opts.ContainerName);
    }

    public async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct)
    {
        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
