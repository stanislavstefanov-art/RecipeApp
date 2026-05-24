namespace Recipes.Application.Expenses.ExtractReceipt;

public interface IReceiptExtractionService
{
    Task<ExtractedReceiptDto> ExtractAsync(Stream imageStream, string contentType, CancellationToken cancellationToken);
}
