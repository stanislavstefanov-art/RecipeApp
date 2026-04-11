using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Recipes.SuggestIngredientSubstitutions;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Clients;

public sealed class ClaudeIngredientSubstitutionClient : IClaudeIngredientSubstitutionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeIngredientSubstitutionClient> _logger;
    private readonly ClaudeOptions _options;

    public ClaudeIngredientSubstitutionClient(
        HttpClient httpClient,
        ILogger<ClaudeIngredientSubstitutionClient> logger,
        IOptions<ClaudeOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IngredientSubstitutionSuggestionDto> SuggestAsync(
        IngredientSubstitutionRequestDto request,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Claude API key is missing.");
        }

        var systemPrompt = BuildSystemPrompt(promptTemplate, jsonSchema);
        var userPrompt = BuildUserPrompt(request);

        var payload = new ClaudeMessagesRequest(
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
                            Text: userPrompt)
                    ])
            ]);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation(
            "Calling Claude for ingredient substitution. Ingredient: {Ingredient}, Model: {Model}",
            request.IngredientName,
            _options.Model);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Claude ingredient substitution call failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"Claude ingredient substitution failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var parsed = JsonSerializer.Deserialize<ClaudeMessagesResponse>(responseBody, JsonOptions)
                     ?? throw new InvalidOperationException("Claude response could not be deserialized.");

        var text = ExtractText(parsed);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Claude response did not contain text content.");
        }

        var cleanedJson = StripMarkdownFences(text);

        try
        {
            var result = JsonSerializer.Deserialize<IngredientSubstitutionSuggestionDto>(cleanedJson, JsonOptions);

            if (result is null)
            {
                throw new InvalidOperationException("Claude returned empty JSON content.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude substitution JSON. Raw text: {Text}", text);
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

    private static string BuildUserPrompt(IngredientSubstitutionRequestDto request)
    {
        return $"""
Suggest ingredient substitutions for:

Ingredient: {request.IngredientName}
Recipe context: {request.RecipeContext ?? "N/A"}
Dietary goal: {request.DietaryGoal ?? "N/A"}
""";
    }

    private static string ExtractText(ClaudeMessagesResponse response)
    {
        var textBlocks = response.Content
            .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Text)
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join("\n", textBlocks!);
    }

    private static string StripMarkdownFences(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[7..].Trim();
        }
        else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[3..].Trim();
        }

        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^3].Trim();
        }

        return trimmed;
    }
}