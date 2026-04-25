using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.CritiqueRecipe;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeRecipeCritiqueService : IRecipeCritiqueService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeRecipeCritiqueService> _logger;
    private readonly HttpClient _httpClient;

    // Built once at construction — PromptBuilder composes it from labeled sections + few-shot examples.
    private readonly string _systemPrompt;

    public ClaudeRecipeCritiqueService(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeRecipeCritiqueService> logger)
    {
        _httpClient    = httpClientFactory.CreateClient("ClaudeAgent");
        _options       = options.Value;
        _logger        = logger;
        _systemPrompt  = BuildSystemPrompt();
    }

    public async Task<RecipeCritiqueDto> CritiqueAsync(RecipeDto recipe, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is missing.");

        var recipeJson = JsonSerializer.Serialize(recipe, JsonOptions);

        var request = new ClaudeMessagesRequest(
            Model: _options.Model,
            MaxTokens: 1500,
            System: _systemPrompt,
            Messages:
            [
                new ClaudeMessage(
                    Role: "user",
                    Content: [new ClaudeContentBlock(Type: "text", Text: recipeJson)])
            ]);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(request, JsonOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling Claude recipe critique for recipe {RecipeId}.", recipe.Id);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Claude recipe critique failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"Claude critique failed with status {(int)response.StatusCode}.");
        }

        var parsed = JsonSerializer.Deserialize<ClaudeMessagesResponse>(body, JsonOptions)
                     ?? throw new InvalidOperationException("Claude response could not be deserialized.");

        var text = parsed.Content
            .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Text)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                   ?? throw new InvalidOperationException("Claude response contained no text.");

        return JsonSerializer.Deserialize<RecipeCritiqueDto>(text, JsonOptions)
               ?? throw new InvalidOperationException("Claude critique response could not be deserialized.");
    }

    private static string BuildSystemPrompt() =>
        new PromptBuilder()
            .WithRole(
                """
                You are an expert culinary critic and recipe developer with 20 years of professional kitchen
                experience. You evaluate recipes for home cooks and provide structured, actionable feedback
                across three dimensions: clarity, completeness, and technique.
                """)
            .WithInputSpec(
                """
                You will receive a recipe as JSON with these fields:
                - id: unique identifier (ignore for evaluation purposes)
                - name: recipe name
                - ingredients: array of { name, quantity, unit }
                - steps: array of { order, instruction }

                Evaluate the recipe exactly as written. Do not assume information exists elsewhere.
                """)
            .WithOutputSchema(
                """
                Return ONLY valid JSON with no markdown fences. The JSON must match this schema:
                {
                  "dimensions": [
                    {
                      "dimension": "clarity" | "completeness" | "technique",
                      "rating": integer 1–5,
                      "rationale": string — 1–3 sentences referencing specific recipe content,
                      "suggestions": array of 0–3 concrete improvement strings
                    }
                  ],
                  "summary": string — 2–3 sentence overall assessment,
                  "confidence": "high" | "medium" | "low",
                  "needsReview": boolean
                }
                The "dimensions" array must contain exactly three entries in this order: clarity, completeness, technique.
                """)
            .WithSuccessCriteria(
                "Each dimension rating is an integer between 1 and 5 inclusive.",
                "The dimensions array contains exactly three entries: clarity, completeness, technique — in that order.",
                "Rationale references specific ingredients or step content, not generic observations.",
                "Suggestions are actionable and specific, not generic advice.",
                "needsReview is true when any dimension rating is below 3.")
            .WithFailureModes(
                "If the recipe has no ingredients, rate completeness 1 and set needsReview true.",
                "If the recipe has no steps, rate both clarity and technique 1 and set needsReview true.",
                "If ingredients have no quantities or units (quantity 0, unit empty), penalise completeness by at least 2 points.",
                "If the recipe name is empty or generic (e.g., 'Recipe 1'), note this in the summary.")
            .WithExamples(
                new PromptExample(
                    "Well-structured recipe with precise timing and complete quantities",
                    """
                    {"id":"a1b2","name":"Classic Tomato Pasta","ingredients":[{"name":"spaghetti","quantity":200,"unit":"g"},{"name":"canned tomatoes","quantity":400,"unit":"g"},{"name":"garlic cloves","quantity":3,"unit":""},{"name":"olive oil","quantity":2,"unit":"tbsp"},{"name":"salt","quantity":1,"unit":"tsp"}],"steps":[{"order":1,"instruction":"Boil salted water and cook spaghetti until al dente, about 8–10 minutes."},{"order":2,"instruction":"Sauté crushed garlic in olive oil over medium heat for 1 minute until fragrant."},{"order":3,"instruction":"Add canned tomatoes, simmer uncovered for 15 minutes, season with salt."},{"order":4,"instruction":"Drain pasta and toss with sauce. Serve immediately."}]}
                    """,
                    """
                    {"dimensions":[{"dimension":"clarity","rating":5,"rationale":"Every step includes a precise time (8–10 min, 1 min, 15 min) and observable cue (al dente, fragrant). Action verbs are unambiguous throughout.","suggestions":[]},{"dimension":"completeness","rating":4,"rationale":"All ingredients list weight or volume. The recipe is missing a servings count, which limits meal-planning utility.","suggestions":["Add a servings count (e.g., 'serves 2')."]},{"dimension":"technique","rating":5,"rationale":"Salting the pasta water, controlling sauté heat, simmering uncovered to reduce, and tossing rather than pouring are all correct professional techniques.","suggestions":[]}],"summary":"A well-crafted pasta recipe with clear timing, complete ingredient quantities, and sound technique throughout. Only minor gap is the missing servings count.","confidence":"high","needsReview":false}
                    """),
                new PromptExample(
                    "Incomplete recipe with missing quantities and vague steps",
                    """
                    {"id":"x9y8","name":"Chicken Stew","ingredients":[{"name":"chicken","quantity":0,"unit":""},{"name":"vegetables","quantity":0,"unit":""},{"name":"water","quantity":0,"unit":""}],"steps":[{"order":1,"instruction":"Cook the chicken."},{"order":2,"instruction":"Add vegetables and water."},{"order":3,"instruction":"Cook until done."}]}
                    """,
                    """
                    {"dimensions":[{"dimension":"clarity","rating":1,"rationale":"Steps are critically vague: 'cook the chicken' omits method, temperature, and duration. 'Cook until done' provides no observable doneness cue such as temperature or colour.","suggestions":["Specify method and duration for cooking chicken (e.g., 'Brown chicken pieces over high heat for 5 minutes').","Replace 'until done' with a measurable cue (e.g., 'until internal temperature reaches 75°C')."]},{"dimension":"completeness","rating":1,"rationale":"No ingredient has a quantity or unit. 'Vegetables' is unspecified — a reader cannot shop from or reproduce this recipe.","suggestions":["Add weights or counts for all ingredients (e.g., '500 g chicken thighs').","Replace 'vegetables' with specific names such as carrots, potatoes, and celery."]},{"dimension":"technique","rating":2,"rationale":"No cooking technique is described for any step. The stew structure is implied but there is no mention of browning, deglazing, or controlled simmering.","suggestions":["Add a browning step before adding liquid to develop flavour depth."]}],"summary":"This recipe is too incomplete to reproduce reliably. All three dimensions are critically under-specified; a reader cannot determine portions, method, or doneness. A full rewrite is recommended.","confidence":"high","needsReview":true}
                    """))
            .Build();
}
