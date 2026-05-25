namespace Recipes.Application.Expenses.ExtractReceipt;

public sealed record ExtractedReceiptDto(
    decimal? Amount,
    string? Currency,
    string? Date,
    string? MerchantName,
    IReadOnlyList<ExtractedReceiptItemDto> Items);

public sealed record ExtractedReceiptItemDto(
    string Description,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? TotalPrice);
