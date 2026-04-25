using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Application.Recipes.ScaleRecipe;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeRecipeScalingService : IRecipeScalingService
{
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private const string SystemPrompt =
        """
        You are a culinary assistant that scales recipe ingredient quantities.

        Given a recipe as JSON and a scaling factor, return a scaled version with adjusted ingredient quantities.
        Do NOT change ingredient names, units, or the structure — only multiply each quantity by the factor.

        Return ONLY valid JSON with no markdown fences. The JSON must match this schema exactly:
        {
          "name": string,
          "ingredients": [
            { "name": string, "quantity": positive decimal, "unit": string }
          ]
        }

        Rules:
        - Every ingredient in the input must appear in the output.
        - Quantities must be positive numbers (never zero or negative).
        - Units must be carried over unchanged from the input.
        - Do not add or remove ingredients.
        """;

    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeRecipeScalingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IProvenanceStore _provenanceStore;
    private readonly string _promptVersion;

    public ClaudeRecipeScalingService(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeRecipeScalingService> logger,
        IProvenanceStore provenanceStore)
    {
        _httpClient      = httpClientFactory.CreateClient("ClaudeAgent");
        _options         = options.Value;
        _logger          = logger;
        _provenanceStore = provenanceStore;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(SystemPrompt));
        _promptVersion = Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    public async Task<ScaledRecipeDto> ScaleAsync(
        RecipeDto recipe,
        int fromServings,
        int toServings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is missing.");

        var factor      = (double)toServings / fromServings;
        var recipeJson  = JsonSerializer.Serialize(recipe, JsonOptions);
        var userMessage = $"Scale this recipe from {fromServings} to {toServings} servings (factor = {factor:F4}):\n\n{recipeJson}";

        var messages = new List<ClaudeMessage>
        {
            new("user", [new ClaudeContentBlock("text", userMessage)])
        };

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            _logger.LogInformation(
                "Claude recipe scaling attempt {Attempt}/{Max} for recipe {RecipeId}.",
                attempt, MaxAttempts, recipe.Id);

            var rawResponse = await SendAsync(messages, cancellationToken);

            var (dto, errors) = TryParseResponse(rawResponse, recipe.Id, recipe.Name, fromServings, toServings, attempt);

            if (errors.Count == 0)
            {
                var provenanceId = _provenanceStore.Record("recipe-scaling", _options.Model, _promptVersion);
                return dto! with { ProvenanceId = provenanceId };
            }

            _logger.LogWarning(
                "Attempt {Attempt} validation failed for recipe {RecipeId}. Errors: {Errors}",
                attempt, recipe.Id, string.Join("; ", errors));

            if (attempt < MaxAttempts)
            {
                // Append Claude's response and the error feedback as the next user turn.
                // Claude will see its own previous output alongside the specific errors to fix.
                messages.Add(new ClaudeMessage("assistant", [new ClaudeContentBlock("text", rawResponse)]));
                messages.Add(new ClaudeMessage("user",
                    [new ClaudeContentBlock("text",
                        $"Your response had the following validation errors:\n{string.Join("\n", errors.Select(e => $"- {e}"))}\n\nPlease correct your JSON and resubmit.")]));
            }
        }

        throw new InvalidOperationException(
            $"Claude failed to return a valid scaled recipe for '{recipe.Name}' after {MaxAttempts} attempts.");
    }

    private async Task<string> SendAsync(IReadOnlyList<ClaudeMessage> messages, CancellationToken ct)
    {
        var request = new ClaudeMessagesRequest(
            Model: _options.Model,
            MaxTokens: 1000,
            System: SystemPrompt,
            Messages: messages);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(request, JsonOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Claude scaling call failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException($"Claude scaling failed with status {(int)response.StatusCode}.");
        }

        var parsed = JsonSerializer.Deserialize<ClaudeMessagesResponse>(body, JsonOptions)
                     ?? throw new InvalidOperationException("Claude response could not be deserialized.");

        return parsed.Content
                   .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
                   .Select(x => x.Text)
                   .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
               ?? throw new InvalidOperationException("Claude response contained no text.");
    }

    private static (ScaledRecipeDto? dto, IReadOnlyList<string> errors) TryParseResponse(
        string rawJson,
        Guid recipeId,
        string recipeName,
        int fromServings,
        int toServings,
        int attempt)
    {
        var validationErrors = new List<string>();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(rawJson);
        }
        catch (JsonException ex)
        {
            validationErrors.Add($"Response is not valid JSON: {ex.Message}");
            return (null, validationErrors);
        }

        if (root is not JsonObject obj)
        {
            validationErrors.Add("Response root must be a JSON object, not an array or scalar.");
            return (null, validationErrors);
        }

        if (obj["name"] is not JsonValue nameNode || string.IsNullOrWhiteSpace(nameNode.GetValue<string>()))
            validationErrors.Add("Field 'name' is missing or empty.");

        var ingredientsNode = obj["ingredients"];
        if (ingredientsNode is not JsonArray ingredientsArray)
        {
            validationErrors.Add("Field 'ingredients' is missing or not an array.");
            return (null, validationErrors);
        }

        if (ingredientsArray.Count == 0)
            validationErrors.Add("Field 'ingredients' must not be empty.");

        var scaledIngredients = new List<ScaledIngredientDto>();

        for (var i = 0; i < ingredientsArray.Count; i++)
        {
            var item = ingredientsArray[i] as JsonObject;
            if (item is null) { validationErrors.Add($"ingredients[{i}] is not an object."); continue; }

            var name = item["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name))
                validationErrors.Add($"ingredients[{i}].name is missing or empty.");

            decimal quantity;
            try
            {
                quantity = item["quantity"]?.GetValue<decimal>() ?? -1m;
            }
            catch
            {
                validationErrors.Add($"ingredients[{i}].quantity is not a valid decimal.");
                quantity = -1m;
            }

            if (quantity <= 0)
                validationErrors.Add($"ingredients[{i}].quantity must be a positive number (got {quantity}).");

            var unit = item["unit"]?.GetValue<string>() ?? string.Empty;

            if (validationErrors.Count == 0)
                scaledIngredients.Add(new ScaledIngredientDto(name!, quantity, unit));
        }

        if (validationErrors.Count > 0)
            return (null, validationErrors);

        return (new ScaledRecipeDto(recipeId, recipeName, fromServings, toServings, scaledIngredients, attempt, Guid.Empty),
                Array.Empty<string>());
    }
}
