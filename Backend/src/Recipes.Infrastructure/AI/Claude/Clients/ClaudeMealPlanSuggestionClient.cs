using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Domain.Enums;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Clients;

public sealed class ClaudeMealPlanSuggestionClient : IClaudeMealPlanSuggestionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeMealPlanSuggestionClient> _logger;
    private readonly ClaudeOptions _options;

    public ClaudeMealPlanSuggestionClient(
        HttpClient httpClient,
        ILogger<ClaudeMealPlanSuggestionClient> logger,
        IOptions<ClaudeOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<MealPlanSuggestionDto> SuggestAsync(
        MealPlanSuggestionRequestDto request,
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
            MaxTokens: 8000,
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
            "Calling Claude for meal plan suggestion. Name: {Name}, Days: {Days}, MealTypeCount: {MealTypeCount}, RecipeCount: {RecipeCount}, Model: {Model}",
            request.Name,
            request.NumberOfDays,
            request.MealTypes.Count,
            request.AvailableRecipes.Count,
            _options.Model);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Claude meal plan suggestion failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"Claude meal plan suggestion failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var parsed = JsonSerializer.Deserialize<ClaudeMessagesResponse>(responseBody, JsonOptions)
                     ?? throw new InvalidOperationException("Claude response could not be deserialized.");

        var cleanedJson = ClaudeResponseParser.StripMarkdownFences(ClaudeResponseParser.ExtractText(parsed));

        if (string.IsNullOrWhiteSpace(cleanedJson))
        {
            throw new InvalidOperationException("Claude response did not contain text content.");
        }

        try
        {
            var result = JsonSerializer.Deserialize<MealPlanSuggestionDto>(cleanedJson, JsonOptions);

            if (result is null)
            {
                throw new InvalidOperationException("Claude returned empty JSON content.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude meal plan JSON. Raw text: {Text}", cleanedJson);
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

    private static string BuildUserPrompt(MealPlanSuggestionRequestDto request)
    {
        var recipes = string.Join(
            "\n",
            request.AvailableRecipes.Select(recipe =>
            {
                var variations = recipe.Variations.Count == 0
                    ? "None"
                    : string.Join(
                        "\n",
                        recipe.Variations.Select(v =>
                            $"    - VariationId: {v.RecipeVariationId}, Name: {v.Name}, Notes: {v.Notes ?? "N/A"}, IngredientAdjustmentNotes: {v.IngredientAdjustmentNotes ?? "N/A"}"));

                var recipeTypeName = recipe.RecipeType == (int)Domain.Enums.RecipeType.Salad ? "Salad" : "MainDish";

                return $"""
        - RecipeId: {recipe.RecipeId}
        Name: {recipe.Name}
        RecipeType: {recipeTypeName}
        Variations:
        {variations}
        """;
            }));

        var mealTypes = string.Join(", ", request.MealTypes.Select(m => ((MealType)m).ToString()));

        var householdMembers = string.Join(
            "\n",
            request.Household.Members.Select(member =>
                $"""
                - PersonId: {member.PersonId}
                Name: {member.Name}
                DietaryPreferences: {(member.DietaryPreferences.Count == 0 ? "None" : string.Join(", ", member.DietaryPreferences.Select(p => ((DietaryPreference)p).ToString())))}
                HealthConcerns: {(member.HealthConcerns.Count == 0 ? "None" : string.Join(", ", member.HealthConcerns.Select(h => ((HealthConcern)h).ToString())))}
                Notes: {member.Notes ?? "N/A"}
                """));

        return $"""
            Suggest a realistic household meal plan.

            Meal plan name: {request.Name}
            HouseholdId: {request.Household.HouseholdId}
            HouseholdName: {request.Household.HouseholdName}
            Start date: {request.StartDate:yyyy-MM-dd}
            Number of days: {request.NumberOfDays}
            Meal types: {mealTypes}

            Household members:
            {householdMembers}

            Available recipes:
            {recipes}
            """;
    }

}