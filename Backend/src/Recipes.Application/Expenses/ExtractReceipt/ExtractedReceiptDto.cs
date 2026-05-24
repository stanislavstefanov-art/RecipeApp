namespace Recipes.Application.Expenses.ExtractReceipt;

public sealed record ExtractedReceiptDto(
    decimal? Amount,
    string? Currency,
    string? Date,
    string? MerchantName);
