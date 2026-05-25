using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Expenses.ExtractReceipt;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Azure;

public sealed class AzureReceiptExtractionService : IReceiptExtractionService
{
    private readonly DocumentAnalysisClient _client;
    private readonly ILogger<AzureReceiptExtractionService> _logger;

    public AzureReceiptExtractionService(
        IOptions<ReceiptExtractionOptions> options,
        ILogger<AzureReceiptExtractionService> logger)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.Endpoint))
            throw new InvalidOperationException(
                "ReceiptExtraction:Endpoint is not configured. Set ReceiptExtraction__Endpoint in the app settings.");
        _client = new DocumentAnalysisClient(
            new Uri(opts.Endpoint),
            new AzureKeyCredential(opts.ApiKey));
        _logger = logger;
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
        {
            _logger.LogWarning("Azure DI returned no documents for the uploaded image.");
            throw new InvalidOperationException(
                "No receipt was recognized in the image. Make sure the photo shows a clear, readable receipt.");
        }

        // Log every field so we can diagnose extraction issues in Application Insights.
        foreach (var (key, field) in doc.Fields)
            _logger.LogInformation("ReceiptField {Key}: type={Type} value={Value}",
                key, field.FieldType, field.Content);

        decimal? amount = null;
        string? currency = null;
        string? date = null;
        string? merchantName = null;

        if (doc.Fields.TryGetValue("Total", out var totalField))
        {
            if (totalField.FieldType == DocumentFieldType.Currency)
            {
                var cv = totalField.Value.AsCurrency();
                amount = (decimal)cv.Amount;
                if (!string.IsNullOrWhiteSpace(cv.Symbol))
                    currency = CurrencySymbolToCode(cv.Symbol);
            }
            else if (totalField.FieldType == DocumentFieldType.Double)
            {
                amount = (decimal)totalField.Value.AsDouble();
                // Try to derive the currency symbol from the raw content (e.g. "$31.3").
                var content = totalField.Content?.Trim();
                if (!string.IsNullOrEmpty(content))
                    currency = CurrencySymbolToCode(ExtractLeadingSymbol(content));
            }
        }

        // Top-level "Currency" field (ISO code) overrides any symbol-derived value.
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

        var items = ExtractItems(doc);

        return new ExtractedReceiptDto(amount, currency, date, merchantName, items);
    }

    private static IReadOnlyList<ExtractedReceiptItemDto> ExtractItems(AnalyzedDocument doc)
    {
        var result = new List<ExtractedReceiptItemDto>();

        if (!doc.Fields.TryGetValue("Items", out var itemsField)
            || itemsField.FieldType != DocumentFieldType.List)
            return result;

        foreach (var itemField in itemsField.Value.AsList())
        {
            if (itemField.FieldType != DocumentFieldType.Dictionary) continue;
            var dict = itemField.Value.AsDictionary();

            string? description = null;
            decimal? quantity = null;
            decimal? unitPrice = null;
            decimal? totalPrice = null;

            if (dict.TryGetValue("Description", out var descField)
                && descField.FieldType == DocumentFieldType.String)
                description = descField.Value.AsString();

            if (dict.TryGetValue("Quantity", out var qtyField)
                && qtyField.FieldType == DocumentFieldType.Double)
                quantity = (decimal)qtyField.Value.AsDouble();

            if (dict.TryGetValue("Price", out var priceField))
            {
                if (priceField.FieldType == DocumentFieldType.Currency)
                    unitPrice = (decimal)priceField.Value.AsCurrency().Amount;
                else if (priceField.FieldType == DocumentFieldType.Double)
                    unitPrice = (decimal)priceField.Value.AsDouble();
            }

            if (dict.TryGetValue("TotalPrice", out var totalPriceField))
            {
                if (totalPriceField.FieldType == DocumentFieldType.Currency)
                    totalPrice = (decimal)totalPriceField.Value.AsCurrency().Amount;
                else if (totalPriceField.FieldType == DocumentFieldType.Double)
                    totalPrice = (decimal)totalPriceField.Value.AsDouble();
            }

            if (description is not null)
                result.Add(new ExtractedReceiptItemDto(description, quantity, unitPrice, totalPrice));
        }

        return result;
    }

    // Returns the leading non-digit, non-space prefix of a string like "$31.3" → "$".
    private static string ExtractLeadingSymbol(string content)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in content)
        {
            if (char.IsDigit(c) || c == '.' || c == ',') break;
            sb.Append(c);
        }
        return sb.ToString().Trim();
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
