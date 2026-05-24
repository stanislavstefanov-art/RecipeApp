# D1 — Receipt Extraction: Azure AI Document Intelligence

## What this implements

Receipt scanning on the expenses screen. The user uploads a photo or PDF of a
receipt; the backend sends it to Azure AI Document Intelligence using the
`prebuilt-receipt` model and returns extracted fields (total amount, currency,
transaction date, merchant name) that pre-fill the "Add expense" form.

Entry point: `POST /api/expenses/extract-receipt`

## Provider switch

`ReceiptExtraction:Provider` in appsettings controls which implementation runs:

| Value   | Class                          | When to use                                 |
|---------|-------------------------------|---------------------------------------------|
| `Azure` | `AzureReceiptExtractionService`| Production — real Document Intelligence F0  |
| `Stub`  | `StubReceiptExtractionService` | Local dev / CI — no Azure credentials needed|

## Azure AI Document Intelligence coverage

| Aspect | Where in code |
|---|---|
| Prebuilt receipt model (`prebuilt-receipt`) | `AzureReceiptExtractionService.ExtractAsync` |
| `DocumentAnalysisClient` + `AzureKeyCredential` | Constructor — Endpoint and ApiKey from `ReceiptExtractionOptions` |
| `WaitUntil.Completed` polling | `AnalyzeDocumentAsync` call |
| Field extraction (`Total`, `Currency`, `TransactionDate`, `MerchantName`) | `doc.Fields.TryGetValue` with type-safe `.AsCurrency()`, `.AsString()`, `.AsDate()` |
| Null-safe extraction (model may not detect all fields) | All fields are `nullable` in `ExtractedReceiptDto` |

## Azure setup (production)

1. Create a **Document Intelligence** resource (F0 free tier) in the Azure portal.
2. Copy the **Endpoint** and **Key 1** from the resource's Keys and Endpoint blade.
3. Set as Azure App Service environment variables:
   ```
   ReceiptExtraction__Provider=Azure
   ReceiptExtraction__Endpoint=https://<name>.cognitiveservices.azure.com/
   ReceiptExtraction__ApiKey=<key>
   ```

## Free tier limits (F0)

- 500 pages/month
- 2 transactions/second
- No custom model training (prebuilt models only on F0)
