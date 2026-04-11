using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Clients;

public sealed class ClaudeRecipeImportClient : IClaudeRecipeImportClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeRecipeImportClient> _logger;
    private readonly HttpClient _httpClient;

    public ClaudeRecipeImportClient(
        HttpClient httpClient,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeRecipeImportClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RecipeExtractionResult> ImportAsync(
        string inputText,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Claude API key is missing.");
        }

        var systemPrompt = BuildSystemPrompt(promptTemplate, jsonSchema);

        var request = new ClaudeMessagesRequest(
            Model: _options.Model,
            MaxTokens: _options.MaxTokens,
            System: systemPrompt,
            Messages:
            [
                new ClaudeMessage(
                    Role: "user",
                    Content:
                    [
                        new ClaudeContentBlock(
                            Type: "text",
                            Text: inputText)
                    ])
            ]);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(request, JsonOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation(
            "Calling Claude recipe import. Model: {Model}, InputLength: {InputLength}",
            _options.Model,
            inputText.Length);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Claude recipe import failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"Claude recipe import failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var parsed = JsonSerializer.Deserialize<ClaudeMessagesResponse>(responseBody, JsonOptions)
                     ?? throw new InvalidOperationException("Claude response could not be deserialized.");

        var text = ExtractText(parsed);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Claude response did not contain text content.");
        }

        try
        {
            var result = JsonSerializer.Deserialize<RecipeExtractionResult>(text, JsonOptions);

            if (result is null)
            {
                throw new InvalidOperationException("Claude returned empty JSON content.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude JSON response. Raw text: {Text}", text);
            throw;
        }
    }

    private static string BuildSystemPrompt(string promptTemplate, string jsonSchema)
    {
        return $"""
{promptTemplate}

Return only valid JSON.
Do not wrap the JSON in markdown fences.
The JSON must match this schema exactly:

{jsonSchema}
""";
    }

    private static string ExtractText(ClaudeMessagesResponse response)
    {
        var textBlocks = response.Content
            .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Text)
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join("\n", textBlocks);
    }
}