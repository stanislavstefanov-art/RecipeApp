using Recipes.Application.Expenses.ExtractReceipt;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubReceiptExtractionService : IReceiptExtractionService
{
    public Task<ExtractedReceiptDto> ExtractAsync(Stream imageStream, string contentType, CancellationToken cancellationToken)
    {
        var dto = new ExtractedReceiptDto(
            Amount: 42.50m,
            Currency: "USD",
            Date: DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            MerchantName: "Stub Receipt — Supermarket");

        return Task.FromResult(dto);
    }
}
