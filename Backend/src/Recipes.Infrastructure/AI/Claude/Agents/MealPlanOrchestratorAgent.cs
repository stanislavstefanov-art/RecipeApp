using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Application.MealPlans.SuggestMealPlanMultiAgent;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Agents;

public sealed class MealPlanOrchestratorAgent : IMealPlanOrchestratorAgent
{
    private const string DecomposeSystemPrompt = """
        You are a meal planning orchestrator. Analyse the household profile and
        the week parameters, then call decompose_task with brief guidance that will
        steer the recipe discovery and assignment sub-agents.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly RecipeDiscoverySubAgent _discoveryAgent;
    private readonly MealAssignmentSubAgent _assignmentAgent;
    private readonly HttpClient _claudeClient;
    private readonly ClaudeOptions _options;
    private readonly ILogger<MealPlanOrchestratorAgent> _logger;

    public MealPlanOrchestratorAgent(
        RecipeDiscoverySubAgent discoveryAgent,
        MealAssignmentSubAgent assignmentAgent,
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        ILogger<MealPlanOrchestratorAgent> logger)
    {
        _discoveryAgent  = discoveryAgent;
        _assignmentAgent = assignmentAgent;
        _claudeClient    = httpClientFactory.CreateClient("ClaudeAgent");
        _options         = options.Value;
        _logger          = logger;
    }

    public async Task<ErrorOr<MealPlanSuggestionDto>> RunAsync(
        SuggestMealPlanMultiAgentCommand command,
        HouseholdPlanningProfileDto household,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "MealPlanOrchestratorAgent: household={Household}, days={Days}",
            household.HouseholdName, command.NumberOfDays);

        // ── Phase 1: Decompose ────────────────────────────────────────────
        _logger.LogInformation("MealPlanOrchestratorAgent: Phase 1 — decompose");
        var decomposition = await DecomposeAsync(command, household, ct);

        _logger.LogInformation(
            "MealPlanOrchestratorAgent: decomposed → focus={Focus}, diversity={Diversity}, servings={Servings}",
            decomposition.RecipeFocus, decomposition.DiversityGoal, decomposition.ServingsPerMeal);

        // ── Phase 2: Recipe Discovery (sub-agent) ─────────────────────────
        _logger.LogInformation("MealPlanOrchestratorAgent: Phase 2 — recipe discovery sub-agent");
        var discoveryCtx = new RecipeDiscoveryContext(
            household, decomposition, command.NumberOfDays, command.MealTypes);

        var candidates = await _discoveryAgent.RunAsync(discoveryCtx, ct);

        _logger.LogInformation(
            "MealPlanOrchestratorAgent: discovery returned {Count} candidates", candidates.Count);

        if (candidates.Count == 0)
            return Error.Failure("MealPlan.NoCandidates", "Recipe discovery returned no candidates.");

        // ── Phase 3: Meal Assignment (sub-agent) ──────────────────────────
        _logger.LogInformation("MealPlanOrchestratorAgent: Phase 3 — meal assignment sub-agent");
        var assignmentCtx = new MealAssignmentContext(
            candidates, household, command.StartDate, command.NumberOfDays, command.MealTypes);

        var plan = await _assignmentAgent.RunAsync(assignmentCtx, command.Name, ct);

        _logger.LogInformation(
            "MealPlanOrchestratorAgent: complete — {Count} entries", plan.Entries.Count);

        return plan;
    }

    // ── Phase 1 decompose (single forced-tool Claude call) ─────────────────

    private async Task<TaskDecomposition> DecomposeAsync(
        SuggestMealPlanMultiAgentCommand command,
        HouseholdPlanningProfileDto household,
        CancellationToken ct)
    {
        var userMessage = $"""
            Plan a {command.NumberOfDays}-day meal plan for household '{household.HouseholdName}'.
            Meal types: {string.Join(", ", command.MealTypes)}.
            Members ({household.Members.Count}): {string.Join(", ", household.Members.Select(m => m.Name))}.
            Dietary preferences: {string.Join("; ", household.Members.Select(m =>
                $"{m.Name}: {(m.DietaryPreferences.Count > 0 ? string.Join(",", m.DietaryPreferences) : "none")}"))}
            """;

        var request = new ClaudeAgentRequest(
            Model:      _options.Model,
            MaxTokens:  512,
            System:     DecomposeSystemPrompt,
            Tools:      [MealPlanAgentTools.DecomposeTask],
            ToolChoice: new ClaudeToolChoiceForced("decompose_task"),
            Messages:   [new ClaudeAgentMessage("user",
                [new ClaudeAgentContentBlock("text", Text: userMessage)])]);

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
            _logger.LogError("Claude decompose error {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Claude decompose error {(int)response.StatusCode}: {body}");
        }

        var claudeResponse = JsonSerializer.Deserialize<ClaudeAgentResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Empty decompose response.");

        var toolUse = claudeResponse.Content.FirstOrDefault(b => b.Type == "tool_use" && b.Name == "decompose_task")
            ?? throw new InvalidOperationException("Claude did not call decompose_task.");

        var input = JsonSerializer.Deserialize<DecomposeTaskInput>(toolUse.Input!.Value.GetRawText(), JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialise decompose_task input.");

        return new TaskDecomposition(input.RecipeFocus, input.DiversityGoal, input.ServingsPerMeal);
    }
}
