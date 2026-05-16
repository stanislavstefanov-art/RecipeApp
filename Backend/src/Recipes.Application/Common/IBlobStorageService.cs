namespace Recipes.Application.Common;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct);
    Task DeleteAsync(string blobName, CancellationToken ct);
}
