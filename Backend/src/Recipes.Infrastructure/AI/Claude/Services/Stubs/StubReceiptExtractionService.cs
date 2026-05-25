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
            MerchantName: "Stub Receipt — Supermarket",
            Items:
            [
                new ExtractedReceiptItemDto("Whole Milk 1L", 2m, 1.49m, 2.98m),
                new ExtractedReceiptItemDto("Sourdough Bread", 1m, 3.25m, 3.25m),
                new ExtractedReceiptItemDto("Organic Eggs x12", 1m, 4.99m, 4.99m),
                new ExtractedReceiptItemDto("Cheddar Cheese 250g", 1m, 2.79m, 2.79m),
            ]);

        return Task.FromResult(dto);
    }
}
