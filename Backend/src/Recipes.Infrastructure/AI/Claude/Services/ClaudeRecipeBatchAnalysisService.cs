using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Recipes.BatchAnalyseRecipes;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeRecipeBatchAnalysisService : IRecipeBatchAnalysisService
{
    private const string BatchesUrl = "https://api.anthropic.com/v1/messages/batches";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    // Placed once as the cached system block. All batch items share this prompt, so
    // only the first item in each batch writes to the cache; the rest read from it.
    // The cache_control breakpoint is set on this block — everything up to and
    // including it will be stored in Anthropic's prompt cache.
    private static readonly IReadOnlyList<ClaudeCachedSystemBlock> CachedSystemBlocks =
    [
        new ClaudeCachedSystemBlock(
            Type: "text",
            Text:
            """
            You are a culinary quality assessor. For each recipe submitted to you, produce a concise
            quality assessment in JSON format.

            Evaluate the recipe across these dimensions:
            - Ingredient completeness: are quantities and units present?
            - Step clarity: are instructions precise and ordered?
            - Technique: are the described techniques appropriate for the dish?

            Return ONLY valid JSON with no markdown fences matching this schema:
            {
              "qualityScore": integer 1–10,
              "summary": string — one sentence overall assessment
            }

            Scoring guide:
            - 9–10: Complete, precise, professional-grade recipe
            - 7–8:  Good recipe with minor gaps
            - 5–6:  Usable but missing important detail
            - 3–4:  Significant gaps in either ingredients or steps
            - 1–2:  Critically incomplete; cannot be reproduced
            """,
            CacheControl: new ClaudeCacheControl("ephemeral"))
    ];

    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeRecipeBatchAnalysisService> _logger;
    private readonly HttpClient _httpClient;

    public ClaudeRecipeBatchAnalysisService(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeRecipeBatchAnalysisService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ClaudeAgent");
        _options    = options.Value;
        _logger     = logger;
    }

    public async Task<BatchSubmissionDto> SubmitBatchAsync(
        IReadOnlyList<RecipeDto> recipes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is missing.");

        var batchItems = recipes.Select(recipe => new ClaudeBatchItem(
            CustomId: $"recipe-{recipe.Id}",
            Params: new ClaudeBatchItemParams(
                Model: _options.Model,
                MaxTokens: 256,
                System: CachedSystemBlocks,
                Messages:
                [
                    new ClaudeMessage(
                        Role: "user",
                        Content: [new ClaudeContentBlock("text", JsonSerializer.Serialize(recipe, JsonOptions))])
                ]))).ToList();

        var batchRequest = new ClaudeBatchRequest(batchItems);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BatchesUrl);
        AddClaudeHeaders(httpRequest);

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(batchRequest, JsonOptions),
            Encoding.UTF8, "application/json");

        _logger.LogInformation(
            "Submitting batch of {Count} recipe analysis requests to Anthropic Batch API.",
            recipes.Count);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Batch submit failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"Batch submission failed with status {(int)response.StatusCode}.");
        }

        var batchStatus = JsonSerializer.Deserialize<ClaudeBatchStatusResponse>(body, JsonOptions)
                          ?? throw new InvalidOperationException("Batch response could not be deserialized.");

        _logger.LogInformation(
            "Batch {BatchId} submitted. Status: {Status}.",
            batchStatus.Id, batchStatus.ProcessingStatus);

        return new BatchSubmissionDto(batchStatus.Id, batchStatus.ProcessingStatus, recipes.Count);
    }

    public async Task<BatchResultsDto> GetResultsAsync(string batchId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is missing.");

        var statusUrl  = $"{BatchesUrl}/{batchId}";
        var resultsUrl = $"{BatchesUrl}/{batchId}/results";

        // Check current batch status.
        using var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
        AddClaudeHeaders(statusRequest);

        using var statusResponse = await _httpClient.SendAsync(statusRequest, cancellationToken);
        var statusBody = await statusResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!statusResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Batch status check failed with status {(int)statusResponse.StatusCode}.");
        }

        var batchStatus = JsonSerializer.Deserialize<ClaudeBatchStatusResponse>(statusBody, JsonOptions)
                          ?? throw new InvalidOperationException("Batch status could not be deserialized.");

        if (batchStatus.ProcessingStatus != "ended")
        {
            _logger.LogInformation(
                "Batch {BatchId} still processing. Counts: {Counts}",
                batchId, batchStatus.RequestCounts);
            return new BatchResultsDto(batchId, batchStatus.ProcessingStatus, null, null);
        }

        // Batch has ended — fetch JSONL results.
        using var resultsRequest = new HttpRequestMessage(HttpMethod.Get, resultsUrl);
        AddClaudeHeaders(resultsRequest);

        using var resultsResponse = await _httpClient.SendAsync(resultsRequest, cancellationToken);
        var resultsBody = await resultsResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!resultsResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Batch results fetch failed with status {(int)resultsResponse.StatusCode}.");
        }

        var analysisResults = new List<RecipeAnalysisResultDto>();
        var cacheStats = new BatchCacheStatsDto(0, 0, 0, 0);
        int totalInput = 0, totalOutput = 0, totalCacheCreation = 0, totalCacheRead = 0;

        // Results are returned as JSONL — one JSON object per line.
        foreach (var line in resultsBody.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var resultLine = JsonSerializer.Deserialize<ClaudeBatchResultLine>(line, JsonOptions);
            if (resultLine is null) continue;

            // custom_id format: "recipe-{guid}"
            var recipeId = Guid.TryParse(resultLine.CustomId.Replace("recipe-", ""), out var parsed)
                ? parsed
                : Guid.Empty;

            if (resultLine.Result.Type == "errored")
            {
                analysisResults.Add(new RecipeAnalysisResultDto(
                    recipeId, string.Empty, false, null, null,
                    resultLine.Result.Error?.Message ?? "Unknown error"));
                continue;
            }

            var message = resultLine.Result.Message;
            if (message is null) continue;

            // Accumulate cache token statistics across all result items.
            if (message.Usage is { } usage)
            {
                totalInput         += usage.InputTokens;
                totalOutput        += usage.OutputTokens;
                totalCacheCreation += usage.CacheCreationInputTokens ?? 0;
                totalCacheRead     += usage.CacheReadInputTokens     ?? 0;
            }

            var text = message.Content
                .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Text)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            if (string.IsNullOrWhiteSpace(text))
            {
                analysisResults.Add(new RecipeAnalysisResultDto(
                    recipeId, string.Empty, false, null, null, "Empty response from Claude."));
                continue;
            }

            int?   qualityScore = null;
            string? summary     = null;

            try
            {
                var root = JsonNode.Parse(text);
                qualityScore = root?["qualityScore"]?.GetValue<int>();
                summary      = root?["summary"]?.GetValue<string>();
            }
            catch (JsonException)
            {
                // Treat parse failure as a non-fatal per-item error.
            }

            analysisResults.Add(new RecipeAnalysisResultDto(
                recipeId, string.Empty, true, qualityScore, summary, null));
        }

        cacheStats = new BatchCacheStatsDto(totalInput, totalOutput, totalCacheCreation, totalCacheRead);

        _logger.LogInformation(
            "Batch {BatchId} results parsed. Items: {Count}. Cache — created: {Created}, read: {Read}.",
            batchId, analysisResults.Count, totalCacheCreation, totalCacheRead);

        return new BatchResultsDto(batchId, "ended", analysisResults, cacheStats);
    }

    private void AddClaudeHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Add("anthropic-beta", "message-batches-2024-09-24");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
