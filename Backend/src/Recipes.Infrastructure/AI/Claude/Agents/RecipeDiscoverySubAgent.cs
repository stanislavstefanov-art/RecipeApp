using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Common.AI;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Agents;

public sealed class RecipeDiscoverySubAgent
{
    private const int MaxIterations = 10;

    private const string SystemPrompt = """
        You are a recipe discovery agent. Your task is to select a diverse, suitable set
        of recipes for a household's weekly meal plan.

        Work step by step:
        1. Call get_recent_meal_history (days=14) to see what has been eaten recently.
        2. Call list_available_recipes to see all options.
        3. Optionally call get_recipe_details on recipes you want to inspect more closely.
        4. Call submit_candidates with your recommendations.

        Aim for at least (days × mealTypes) candidates so the assignment agent has
        enough variety. Consider dietary preferences, health concerns, and the task guidance.
        Avoid repeating recipes from recent history unless unavoidable.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _claudeClient;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly ClaudeOptions _options;
    private readonly ILogger<RecipeDiscoverySubAgent> _logger;
    private readonly IToolCallTelemetry _telemetry;

    public RecipeDiscoverySubAgent(
        IHttpClientFactory httpClientFactory,
        IRecipeRepository recipeRepository,
        IMealPlanRepository mealPlanRepository,
        IOptions<ClaudeOptions> options,
        ILogger<RecipeDiscoverySubAgent> logger,
        IToolCallTelemetry telemetry)
    {
        _claudeClient      = httpClientFactory.CreateClient("ClaudeAgent");
        _recipeRepository  = recipeRepository;
        _mealPlanRepository = mealPlanRepository;
        _options           = options.Value;
        _logger            = logger;
        _telemetry         = telemetry;
    }

    internal async Task<IReadOnlyList<RecipeCandidate>> RunAsync(
        RecipeDiscoveryContext ctx,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "RecipeDiscoverySubAgent: household={Household}, days={Days}, mealTypes={MealTypes}",
            ctx.Household.HouseholdName, ctx.DaysCount, string.Join(",", ctx.MealTypes));

        var userMessage = $"""
            Plan recipes for {ctx.DaysCount} day(s) with meal types: {string.Join(", ", ctx.MealTypes)}.

            Household: {ctx.Household.HouseholdName}
            Members: {string.Join(", ", ctx.Household.Members.Select(m => $"{m.Name} (id:{m.PersonId})"))}

            Task guidance:
            - Recipe focus: {ctx.Decomposition.RecipeFocus}
            - Diversity goal: {ctx.Decomposition.DiversityGoal}
            - Servings per meal: {ctx.Decomposition.ServingsPerMeal}

            Please discover and submit suitable recipe candidates.
            """;

        var messages = new List<ClaudeAgentMessage>
        {
            new("user", [new ClaudeAgentContentBlock("text", Text: userMessage)])
        };

        var state = new AgentState();

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            var response = await CallClaudeAsync(messages, MealPlanAgentTools.DiscoveryTools, ct);

            _logger.LogInformation(
                "RecipeDiscoverySubAgent iteration {Iter}: {Stop}, in={In} out={Out}",
                iteration + 1, response.StopReason,
                response.Usage?.InputTokens, response.Usage?.OutputTokens);

            messages.Add(new("assistant", response.Content));

            if (response.StopReason != "tool_use")
                break;

            var toolUseBlocks = response.Content.Where(b => b.Type == "tool_use").ToList();

            var toolResults = await Task.WhenAll(
                toolUseBlocks.Select(b => InstrumentedDispatchAsync(b, state, ctx, response.Usage, ct)));

            if (state.Complete)
                return state.Candidates;

            messages.Add(new("user", toolResults));
        }

        if (state.Candidates.Count > 0)
            return state.Candidates;

        throw new InvalidOperationException("RecipeDiscoverySubAgent ended without calling submit_candidates.");
    }

    // ── Instrumented dispatch ──────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> InstrumentedDispatchAsync(
        ClaudeAgentContentBlock toolUse,
        AgentState state,
        RecipeDiscoveryContext ctx,
        ClaudeUsage? usage,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var result = await DispatchToolAsync(toolUse, state, ctx, ct);
        sw.Stop();
        _telemetry.Record(new ToolCallRecord
        {
            AgentName           = "RecipeDiscoverySubAgent",
            ToolName            = toolUse.Name ?? "unknown",
            InputBytes          = toolUse.Input?.GetRawText().Length ?? 0,
            OutputBytes         = result.Content?.Length ?? 0,
            LatencyMs           = sw.ElapsedMilliseconds,
            IsError             = result.IsError ?? false,
            InputTokens         = usage?.InputTokens,
            OutputTokens        = usage?.OutputTokens,
            CacheReadTokens     = usage?.CacheReadInputTokens,
            CacheCreationTokens = usage?.CacheCreationInputTokens,
        });
        return result;
    }

    // ── Tool dispatcher ────────────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> DispatchToolAsync(
        ClaudeAgentContentBlock toolUse,
        AgentState state,
        RecipeDiscoveryContext ctx,
        CancellationToken ct)
    {
        var id = toolUse.Id!;
        return toolUse.Name switch
        {
            "list_available_recipes"  => await ExecuteListRecipesAsync(id, ct),
            "get_recipe_details"      => await ExecuteGetRecipeDetailsAsync(id, Deserialize<GetRecipeDetailsInput>(toolUse.Input!.Value), ct),
            "get_recent_meal_history" => await ExecuteGetRecentHistoryAsync(id, Deserialize<GetRecentMealHistoryInput>(toolUse.Input!.Value), ctx, ct),
            "submit_candidates"       => ExecuteSubmitCandidates(id, Deserialize<SubmitCandidatesInput>(toolUse.Input!.Value), state),
            _                         => ErrorResult(id, $"Unknown tool: {toolUse.Name}"),
        };
    }

    // ── Tool implementations ───────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> ExecuteListRecipesAsync(
        string toolUseId, CancellationToken ct)
    {
        var recipes = await _recipeRepository.GetAllAsync(ct);
        var list = recipes.Select(r => new { id = r.Id.Value, name = r.Name.Value }).ToList();
        return OkResult(toolUseId, JsonSerializer.Serialize(list, JsonOptions));
    }

    private async Task<ClaudeAgentContentBlock> ExecuteGetRecipeDetailsAsync(
        string toolUseId, GetRecipeDetailsInput input, CancellationToken ct)
    {
        if (!Guid.TryParse(input.RecipeId, out var guid))
            return ErrorResult(toolUseId, $"Invalid recipe ID: {input.RecipeId}");

        var recipe = await _recipeRepository.GetByIdAsync(RecipeId.From(guid), ct);
        if (recipe is null)
            return ErrorResult(toolUseId, $"Recipe '{input.RecipeId}' not found.");

        var detail = new
        {
            id          = recipe.Id.Value,
            name        = recipe.Name.Value,
            ingredients = recipe.Ingredients.Select(i => new { name = i.Name, quantity = i.Quantity, unit = i.Unit }),
        };
        return OkResult(toolUseId, JsonSerializer.Serialize(detail, JsonOptions));
    }

    private async Task<ClaudeAgentContentBlock> ExecuteGetRecentHistoryAsync(
        string toolUseId, GetRecentMealHistoryInput input, RecipeDiscoveryContext ctx, CancellationToken ct)
    {
        var cutoff    = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-input.Days));
        var allPlans  = await _mealPlanRepository.GetAllAsync(ct);
        var recentIds = allPlans
            .Where(p => p.HouseholdId.Value == ctx.Household.HouseholdId)
            .SelectMany(p => p.Entries)
            .Where(e => e.PlannedDate >= cutoff)
            .Select(e => e.BaseRecipeId.Value)
            .Distinct()
            .ToList();

        return OkResult(toolUseId, JsonSerializer.Serialize(new { recentRecipeIds = recentIds }, JsonOptions));
    }

    private static ClaudeAgentContentBlock ExecuteSubmitCandidates(
        string toolUseId, SubmitCandidatesInput input, AgentState state)
    {
        state.Candidates = input.Candidates
            .Select(c => new RecipeCandidate(Guid.Parse(c.RecipeId), c.RecipeName, c.Suitability))
            .ToList();
        state.Complete = true;
        return OkResult(toolUseId, JsonSerializer.Serialize(new { accepted = state.Candidates.Count }, JsonOptions));
    }

    // ── Claude HTTP call ───────────────────────────────────────────────────

    private async Task<ClaudeAgentResponse> CallClaudeAsync(
        IReadOnlyList<ClaudeAgentMessage> messages,
        IReadOnlyList<ClaudeToolDefinition> tools,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is missing.");

        var request = new ClaudeAgentRequest(
            Model:      _options.Model,
            MaxTokens:  4096,
            System:     SystemPrompt,
            Tools:      tools,
            ToolChoice: new ClaudeToolChoiceAuto(),
            Messages:   messages);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _claudeClient.SendAsync(httpRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Claude API error {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Claude API error {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<ClaudeAgentResponse>(body, JsonOptions)
               ?? throw new InvalidOperationException("Empty Claude response.");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static T Deserialize<T>(JsonElement el) =>
        JsonSerializer.Deserialize<T>(el.GetRawText(), JsonOptions)!;

    private static ClaudeAgentContentBlock OkResult(string id, string content) =>
        new("tool_result", ToolUseId: id, Content: content);

    private static ClaudeAgentContentBlock ErrorResult(string id, string msg) =>
        new("tool_result", ToolUseId: id, Content: msg, IsError: true);

    private sealed class AgentState
    {
        public List<RecipeCandidate> Candidates { get; set; } = [];
        public bool Complete { get; set; }
    }
}
