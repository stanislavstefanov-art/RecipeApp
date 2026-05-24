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
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-receipt",
            imageStream,
            cancellationToken: cancellationToken);

        var result = operation.Value;
        var doc = result.Documents.FirstOrDefault();
        if (doc is null)
            return new ExtractedReceiptDto(null, null, null, null);

        decimal? amount = null;
        string? currency = null;
        string? date = null;
        string? merchantName = null;

        if (doc.Fields.TryGetValue("Total", out var totalField)
            && totalField.FieldType == DocumentFieldType.Currency)
        {
            var currencyValue = totalField.Value.AsCurrency();
            amount = (decimal)currencyValue.Amount;
        }

        if (doc.Fields.TryGetValue("Currency", out var currencyField)
            && currencyField.FieldType == DocumentFieldType.String)
        {
            currency = currencyField.Value.AsString();
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

        return new ExtractedReceiptDto(amount, currency, date, merchantName);
    }
}
