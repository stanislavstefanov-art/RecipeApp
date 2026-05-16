namespace Recipes.Infrastructure.Options;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string Provider { get; init; } = "Stub";
    public string ContainerName { get; init; } = "recipe-images";
    public string ConnectionString { get; init; } = "";
}
