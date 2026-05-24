namespace Recipes.Infrastructure.Options;

public sealed class ReceiptExtractionOptions
{
    public const string SectionName = "ReceiptExtraction";

    public string Provider { get; init; } = "Stub"; // "Stub" or "Azure"
    public string Endpoint { get; init; } = "";
    public string ApiKey { get; init; } = "";
}
