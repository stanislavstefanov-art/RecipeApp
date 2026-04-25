using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Common.AI;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Agents;

public sealed class MealAssignmentSubAgent
{
    private const int MaxIterations = 15;

    private const string SystemPrompt = """
        You are a meal assignment agent. Assign the provided recipe candidates to
        specific days and meal types for the household's weekly plan.

        Work step by step:
        1. Review the candidates, household members, and the week structure in the user message.
        2. For EACH day × mealType slot, call assign_meal once with a chosen recipe and
           the list of person IDs for that meal.
        3. Once every slot is filled, call finalize_assignments.

        Rules:
        - Cover every day × mealType combination — the system will reject an incomplete plan.
        - Avoid repeating the same recipe on consecutive days where possible.
        - Match recipes to household dietary preferences.
        - Include all household member IDs in personIds unless there is a specific reason to exclude someone.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _claudeClient;
    private readonly ClaudeOptions _options;
    private readonly ILogger<MealAssignmentSubAgent> _logger;
    private readonly IToolCallTelemetry _telemetry;
    private readonly IAgentHookRunner _hookRunner;

    public MealAssignmentSubAgent(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        ILogger<MealAssignmentSubAgent> logger,
        IToolCallTelemetry telemetry,
        IAgentHookRunner hookRunner)
    {
        _claudeClient = httpClientFactory.CreateClient("ClaudeAgent");
        _options      = options.Value;
        _logger       = logger;
        _telemetry    = telemetry;
        _hookRunner   = hookRunner;
    }

    internal async Task<MealPlanSuggestionDto> RunAsync(
        MealAssignmentContext ctx,
        string planName,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "MealAssignmentSubAgent: {Candidates} candidates, {Days} days, {Members} members",
            ctx.Candidates.Count, ctx.DaysCount, ctx.Household.Members.Count);

        var userMessage = $"""
            Assign meals for {ctx.DaysCount} day(s) starting {ctx.StartDate:yyyy-MM-dd}.
            Meal types to cover per day: {string.Join(", ", ctx.MealTypes)}.

            Household members:
            {string.Join("\n", ctx.Household.Members.Select(m => $"  - {m.Name} (id: {m.PersonId})"))}

            Available recipe candidates:
            {string.Join("\n", ctx.Candidates.Select(c => $"  - [{c.RecipeId}] {c.RecipeName}: {c.Suitability}"))}

            Assign every day × mealType slot, then call finalize_assignments.
            """;

        var messages = new List<ClaudeAgentMessage>
        {
            new("user", [new ClaudeAgentContentBlock("text", Text: userMessage)])
        };

        var state = new AgentState(ctx);

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            var response = await CallClaudeAsync(messages, MealPlanAgentTools.AssignmentTools, ct);

            _logger.LogInformation(
                "MealAssignmentSubAgent iteration {Iter}: {Stop}, in={In} out={Out}",
                iteration + 1, response.StopReason,
                response.Usage?.InputTokens, response.Usage?.OutputTokens);

            messages.Add(new("assistant", response.Content));

            if (response.StopReason != "tool_use")
                break;

            var toolUseBlocks = response.Content.Where(b => b.Type == "tool_use").ToList();

            var toolResults = await Task.WhenAll(
                toolUseBlocks.Select(b => InstrumentedDispatchAsync(b, state, response.Usage, iteration, ct)));

            if (state.Complete)
                return BuildDto(planName, state.Entries);

            messages.Add(new("user", toolResults));
        }

        if (state.Entries.Count > 0)
            return BuildDto(planName, state.Entries);

        throw new InvalidOperationException("MealAssignmentSubAgent ended without calling finalize_assignments.");
    }

    // ── Instrumented dispatch ──────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> InstrumentedDispatchAsync(
        ClaudeAgentContentBlock toolUse,
        AgentState state,
        ClaudeUsage? usage,
        int iteration,
        CancellationToken ct)
    {
        var hookCtx  = new AgentHookContext("MealAssignmentSubAgent", toolUse.Name ?? "unknown", toolUse.Input, iteration);
        var decision = await _hookRunner.RunBeforeAsync(hookCtx, ct);

        if (decision is HookDecision.BlockDecision block)
        {
            _telemetry.Record(new ToolCallRecord
            {
                AgentName = "MealAssignmentSubAgent", ToolName = toolUse.Name ?? "unknown",
                InputBytes = toolUse.Input?.GetRawText().Length ?? 0, IsError = true,
                InputTokens = usage?.InputTokens, OutputTokens = usage?.OutputTokens,
                CacheReadTokens = usage?.CacheReadInputTokens, CacheCreationTokens = usage?.CacheCreationInputTokens,
            });
            return ErrorResult(toolUse.Id!, block.Reason);
        }

        var sw     = Stopwatch.StartNew();
        var result = await DispatchToolAsync(toolUse, state, ct);
        sw.Stop();

        _telemetry.Record(new ToolCallRecord
        {
            AgentName           = "MealAssignmentSubAgent",
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

        var outcome = new ToolCallOutcome(result.IsError ?? false, result.Content, sw.ElapsedMilliseconds);
        await _hookRunner.RunAfterAsync(hookCtx, outcome, ct);

        return result;
    }

    // ── Tool dispatcher ────────────────────────────────────────────────────

    private Task<ClaudeAgentContentBlock> DispatchToolAsync(
        ClaudeAgentContentBlock toolUse,
        AgentState state,
        CancellationToken ct)
    {
        var id = toolUse.Id!;
        return Task.FromResult(toolUse.Name switch
        {
            "assign_meal"          => ExecuteAssignMeal(id, Deserialize<AssignMealInput>(toolUse.Input!.Value), state),
            "finalize_assignments" => ExecuteFinalize(id, state),
            _                      => ErrorResult(id, $"Unknown tool: {toolUse.Name}"),
        });
    }

    // ── Tool implementations ───────────────────────────────────────────────

    private static ClaudeAgentContentBlock ExecuteAssignMeal(
        string toolUseId, AssignMealInput input, AgentState state)
    {
        if (!DateOnly.TryParse(input.Date, out var date))
            return ErrorResult(toolUseId, $"Invalid date: {input.Date}");

        if (!Guid.TryParse(input.RecipeId, out var recipeGuid))
            return ErrorResult(toolUseId, $"Invalid recipe ID: {input.RecipeId}");

        var assignments = input.PersonIds
            .Select(pid => Guid.TryParse(pid, out var g)
                ? new MealPlanSuggestionAssignmentDto(g, recipeGuid, null, 1.0m, null)
                : null)
            .Where(a => a is not null)
            .Select(a => a!)
            .ToList();

        state.Entries.Add(new MealPlanSuggestionEntryDto(
            recipeGuid, date, input.MealType, 1, assignments));

        return OkResult(toolUseId, JsonSerializer.Serialize(new { assigned = $"{date:yyyy-MM-dd} mealType={input.MealType}" }, JsonOptions));
    }

    private static ClaudeAgentContentBlock ExecuteFinalize(string toolUseId, AgentState state)
    {
        var expected = Enumerable.Range(0, state.Context.DaysCount)
            .SelectMany(d => state.Context.MealTypes.Select(mt =>
                (state.Context.StartDate.AddDays(d), mt)))
            .ToHashSet();

        var actual = state.Entries
            .Select(e => (e.PlannedDate, e.MealType))
            .ToHashSet();

        var missing = expected.Except(actual).ToList();
        if (missing.Count > 0)
        {
            var desc = string.Join(", ", missing.Select(m => $"{m.Item1:yyyy-MM-dd}/type{m.Item2}"));
            return ErrorResult(toolUseId, $"Incomplete plan — missing {missing.Count} slot(s): {desc}. Assign them before finalizing.");
        }

        state.Complete = true;
        return OkResult(toolUseId, JsonSerializer.Serialize(new { finalized = state.Entries.Count }, JsonOptions));
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

    private static MealPlanSuggestionDto BuildDto(string name, List<MealPlanSuggestionEntryDto> entries) =>
        new(name, entries, Confidence: 0.85, NeedsReview: false, Notes: "Generated by multi-agent orchestrator.");

    private sealed class AgentState(MealAssignmentContext context)
    {
        public MealAssignmentContext Context  { get; } = context;
        public List<MealPlanSuggestionEntryDto> Entries { get; } = [];
        public bool Complete { get; set; }
    }
}
