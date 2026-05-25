using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Options;
using Recipes.Application.Expenses.ExtractReceipt;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Azure;

public sealed class AzureReceiptExtractionService : IReceiptExtractionService
{
    private readonly DocumentAnalysisClient _client;

    public AzureReceiptExtractionService(IOptions<ReceiptExtractionOptions> options)
    {
        var opts = options.Value;
        _client = new DocumentAnalysisClient(
            new Uri(opts.Endpoint),
            new AzureKeyCredential(opts.ApiKey));
    }

    public async Task<ExtractedReceiptDto> ExtractAsync(Stream imageStream, string contentType, CancellationToken cancellationToken)
    {
        // Ensure the stream is at the beginning in case it was read earlier.
        if (imageStream.CanSeek)
            imageStream.Seek(0, SeekOrigin.Begin);

        AnalyzeDocumentOperation operation;
        try
        {
            operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-receipt",
                imageStream,
                cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException(
                ex.ErrorCode == "InvalidContentLength"
                    ? "The image is too large for the receipt scanner (maximum 4 MB for the free tier)."
                    : $"Receipt extraction failed: {ex.Message}",
                ex);
        }

        var result = operation.Value;
        var doc = result.Documents.FirstOrDefault();
        if (doc is null)
            throw new InvalidOperationException(
                "No receipt was recognized in the image. Make sure the photo shows a clear, readable receipt.");

        decimal? amount = null;
        string? currency = null;
        string? date = null;
        string? merchantName = null;

        if (doc.Fields.TryGetValue("Total", out var totalField)
            && totalField.FieldType == DocumentFieldType.Currency)
        {
            var cv = totalField.Value.AsCurrency();
            amount = (decimal)cv.Amount;
            // Prefer the ISO code embedded in the currency value; fall back to the
            // top-level "Currency" string field below if the code is absent.
            if (!string.IsNullOrWhiteSpace(cv.Symbol))
                currency = CurrencySymbolToCode(cv.Symbol);
        }

        // Top-level "Currency" field (ISO code) overrides the symbol-derived value.
        if (doc.Fields.TryGetValue("Currency", out var currencyField)
            && currencyField.FieldType == DocumentFieldType.String)
        {
            var code = currencyField.Value.AsString();
            if (!string.IsNullOrWhiteSpace(code))
                currency = code;
        }

        if (doc.Fields.TryGetValue("TransactionDate", out var dateField)
            && dateField.FieldType == DocumentFieldType.Date)
        {
            date = dateField.Value.AsDate().ToString("yyyy-MM-dd");
        }

        if (doc.Fields.TryGetValue("MerchantName", out var merchantField)
            && merchantField.FieldType == DocumentFieldType.String)
        {
            merchantName = merchantField.Value.AsString();
        }

        if (amount is null && date is null && merchantName is null)
            throw new InvalidOperationException(
                "The receipt was found but no data could be extracted. Try a clearer or better-lit photo.");

        return new ExtractedReceiptDto(amount, currency, date, merchantName);
    }

    private static string CurrencySymbolToCode(string symbol) => symbol.Trim() switch
    {
        "$"  => "USD",
        "€"  => "EUR",
        "£"  => "GBP",
        "¥"  => "JPY",
        "лв" => "BGN",
        _    => symbol,   // already a code or unknown symbol — pass through
    };
}
