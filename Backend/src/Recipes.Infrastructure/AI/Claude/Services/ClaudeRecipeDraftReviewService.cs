using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Application.Recipes.ReviewRecipeDraft;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeRecipeDraftReviewService : IRecipeDraftReviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private const string OutputSchema =
        """
        Return ONLY valid JSON with no markdown fences:
        {
          "verdict": "approve" | "revise" | "reject",
          "confidence": "high" | "medium" | "low",
          "issues": array of strings (empty if verdict is "approve")
        }
        """;

    // Three independent reviewer personas — each brings a different lens to the same draft.
    // Running them concurrently via Task.WhenAll ensures genuinely independent evaluation.
    private static readonly (string Role, string SystemPrompt)[] Reviewers =
    [
        (
            "Ingredient Specialist",
            $"""
            You are an expert recipe editor specialising in ingredient accuracy and completeness.
            Review the submitted recipe draft for ingredient quality.

            Approve if: all ingredients have names, plausible quantities, and appropriate units.
            Revise if: some quantities or units are missing but the recipe is otherwise coherent.
            Reject if: ingredients are so incomplete that the recipe cannot be reproduced.

            {OutputSchema}
            """
        ),
        (
            "Technique Specialist",
            $"""
            You are a professional chef reviewing recipe drafts for technical correctness.
            Review the submitted recipe draft for cooking technique and procedural accuracy.

            Approve if: steps describe correct techniques with appropriate timing and temperatures.
            Revise if: steps are present but lack important technique detail (e.g., temperature, doneness cues).
            Reject if: steps are absent, critically vague, or describe incorrect technique.

            {OutputSchema}
            """
        ),
        (
            "Clarity Specialist",
            $"""
            You are a culinary writing expert reviewing recipe drafts for home-cook accessibility.
            Review the submitted recipe draft for clarity and user experience.

            Approve if: a home cook with basic skills could follow this recipe without confusion.
            Revise if: the recipe is mostly clear but has ambiguous terms or missing context.
            Reject if: the recipe is too vague or contradictory to follow safely.

            {OutputSchema}
            """
        )
    ];

    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeRecipeDraftReviewService> _logger;
    private readonly HttpClient _httpClient;

    public ClaudeRecipeDraftReviewService(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeRecipeDraftReviewService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ClaudeAgent");
        _options    = options.Value;
        _logger     = logger;
    }

    public async Task<RecipeDraftReviewDto> ReviewAsync(
        ImportedRecipeDto draft,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is missing.");

        var draftJson = JsonSerializer.Serialize(draft, JsonOptions);

        // Dispatch all three reviewers concurrently — each is an independent Claude instance
        // with its own system prompt. Task.WhenAll ensures they run in parallel, not serially.
        var reviewTasks = Reviewers
            .Select((reviewer, index) => CallReviewerAsync(reviewer.Role, reviewer.SystemPrompt, draftJson, index, cancellationToken))
            .ToList();

        var jurorVerdicts = await Task.WhenAll(reviewTasks);

        var consensus = AggregateVerdicts(jurorVerdicts);

        _logger.LogInformation(
            "Recipe draft jury completed. Consensus: {Consensus}. Verdicts: {Verdicts}",
            consensus,
            string.Join(", ", jurorVerdicts.Select(v => $"[{v.Role}]={v.Verdict}")));

        return new RecipeDraftReviewDto(draft, consensus, jurorVerdicts);
    }

    private async Task<JurorVerdictDto> CallReviewerAsync(
        string role,
        string systemPrompt,
        string draftJson,
        int index,
        CancellationToken ct)
    {
        var request = new ClaudeMessagesRequest(
            Model: _options.Model,
            MaxTokens: 512,
            System: systemPrompt,
            Messages:
            [
                new ClaudeMessage(
                    Role: "user",
                    Content: [new ClaudeContentBlock("text", draftJson)])
            ]);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json");

        _logger.LogDebug("Juror {Index} ({Role}) dispatched.", index + 1, role);

        using var response = await _httpClient.SendAsync(httpRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Juror {Index} call failed with status {Status}.", index + 1, (int)response.StatusCode);
            return new JurorVerdictDto(index + 1, role, "revise", "low",
                [$"Reviewer call failed with status {(int)response.StatusCode}."]);
        }

        var parsed = JsonSerializer.Deserialize<ClaudeMessagesResponse>(body, JsonOptions);
        var text = parsed?.Content
            .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Text)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (string.IsNullOrWhiteSpace(text))
            return new JurorVerdictDto(index + 1, role, "revise", "low", ["Reviewer returned no text."]);

        return ParseVerdict(text, index + 1, role);
    }

    private static JurorVerdictDto ParseVerdict(string json, int index, string role)
    {
        try
        {
            var root = JsonNode.Parse(json);
            var verdict    = root?["verdict"]?.GetValue<string>()    ?? "revise";
            var confidence = root?["confidence"]?.GetValue<string>() ?? "low";
            var issues     = root?["issues"]?.AsArray()
                                 .Select(x => x?.GetValue<string>() ?? string.Empty)
                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                 .ToList()
                             ?? [];

            // Normalise verdict to one of three accepted values.
            verdict = verdict.ToLowerInvariant() switch
            {
                "approve" => "approve",
                "reject"  => "reject",
                _         => "revise"
            };

            return new JurorVerdictDto(index, role, verdict, confidence, issues);
        }
        catch (JsonException)
        {
            return new JurorVerdictDto(index, role, "revise", "low",
                ["Could not parse reviewer response."]);
        }
    }

    // Majority vote: 2+ identical verdicts wins. Tie → "revise".
    private static string AggregateVerdicts(IReadOnlyList<JurorVerdictDto> verdicts)
    {
        var counts = verdicts
            .GroupBy(v => v.Verdict)
            .ToDictionary(g => g.Key, g => g.Count());

        if (counts.TryGetValue("approve", out var approveCount) && approveCount >= 2) return "approve";
        if (counts.TryGetValue("reject",  out var rejectCount)  && rejectCount  >= 2) return "reject";
        return "revise";
    }
}
