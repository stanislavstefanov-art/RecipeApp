using Recipes.Application.Common;

namespace Recipes.Infrastructure.Storage;

public sealed class StubBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct)
    {
        var seed = Uri.EscapeDataString(blobName);
        return Task.FromResult($"https://picsum.photos/seed/{seed}/800/600");
    }

    public Task DeleteAsync(string blobName, CancellationToken ct) => Task.CompletedTask;
}
