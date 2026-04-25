using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.AnalyseRecipeNutrition;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Mcp;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Agents;

public sealed class NutritionAnalysisAgent : INutritionAnalysisAgent
{
    private const int MaxIterations = 10;

    private const string SystemPrompt = """
        You are a nutrition analysis agent. Use your tools to analyse the nutritional
        content of all ingredients in a recipe.

        Work step by step:
        1. Call get_recipe_ingredients with the recipeId to get the ingredient list.
        2. Call lookup_nutrition for EVERY ingredient — all in the same response to run in parallel.
        3. Call save_nutrition_analysis with the per-ingredient data and the summed totals.

        Compute totals by summing all per-ingredient values (calories, proteinG, carbG, fatG, fiberG).
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _claudeClient;
    private readonly IMcpClientHost _mcpClient;
    private readonly IRecipeRepository _recipeRepository;
    private readonly ClaudeOptions _options;
    private readonly ILogger<NutritionAnalysisAgent> _logger;
    private readonly IToolCallTelemetry _telemetry;

    public NutritionAnalysisAgent(
        IHttpClientFactory httpClientFactory,
        IMcpClientHost mcpClient,
        IRecipeRepository recipeRepository,
        IOptions<ClaudeOptions> options,
        ILogger<NutritionAnalysisAgent> logger,
        IToolCallTelemetry telemetry)
    {
        _claudeClient     = httpClientFactory.CreateClient("ClaudeAgent");
        _mcpClient        = mcpClient;
        _recipeRepository = recipeRepository;
        _options          = options.Value;
        _logger           = logger;
        _telemetry        = telemetry;
    }

    public async Task<ErrorOr<NutritionAnalysisDto>> RunAsync(Guid recipeId, CancellationToken ct)
    {
        // Compose tool list: static tools + MCP-proxied tools
        var mcpTools = await _mcpClient.ListToolsAsync(ct);
        var allTools  = BuildToolList(mcpTools);

        _logger.LogInformation(
            "NutritionAnalysisAgent: {StaticCount} static + {McpCount} MCP tools",
            2, mcpTools.Count);

        var messages = new List<ClaudeAgentMessage>
        {
            new("user", [new ClaudeAgentContentBlock("text",
                Text: $"Analyse nutrition for recipe {recipeId}.")])
        };

        var state = new AgentState();

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            var response = await CallClaudeAsync(messages, allTools, ct);

            _logger.LogInformation(
                "NutritionAnalysisAgent iteration {Iter}: {Stop}, in={In} out={Out}",
                iteration + 1, response.StopReason,
                response.Usage?.InputTokens, response.Usage?.OutputTokens);

            messages.Add(new("assistant", response.Content));

            if (response.StopReason != "tool_use")
            {
                return state.Result is not null
                    ? state.Result
                    : Error.Failure(description: "Agent loop ended without calling save_nutrition_analysis.");
            }

            var toolUseBlocks = response.Content
                .Where(b => b.Type == "tool_use")
                .ToList();

            _logger.LogInformation(
                "Executing {Count} tool(s): {Names}",
                toolUseBlocks.Count,
                string.Join(", ", toolUseBlocks.Select(b => b.Name)));

            // Parallel dispatch — static tools execute locally; MCP tools forward to client
            var toolResults = await Task.WhenAll(
                toolUseBlocks.Select(b => InstrumentedDispatchAsync(b, state, response.Usage, ct)));

            if (state.LoopComplete)
                return state.Result!;

            messages.Add(new("user", toolResults));
        }

        return Error.Failure(description: "Nutrition analysis agent exceeded maximum iterations.");
    }

    // ── Tool list composition ──────────────────────────────────────────────

    private static IReadOnlyList<ClaudeToolDefinition> BuildToolList(
        IReadOnlyList<McpToolDefinition> mcpTools)
    {
        var tools = new List<ClaudeToolDefinition>
        {
            NutritionAnalysisTools.GetRecipeIngredients,
            NutritionAnalysisTools.SaveNutritionAnalysis,
        };

        // Proxy MCP tools into Claude's tools array
        tools.AddRange(mcpTools.Select(t => new ClaudeToolDefinition(
            t.Name,
            t.Description,
            t.InputSchema)));

        return tools;
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

    // ── Tool dispatcher ────────────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> InstrumentedDispatchAsync(
        ClaudeAgentContentBlock toolUse,
        AgentState state,
        ClaudeUsage? usage,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var result = await DispatchToolAsync(toolUse, state, ct);
        sw.Stop();
        _telemetry.Record(new ToolCallRecord
        {
            AgentName           = "NutritionAnalysisAgent",
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

    private async Task<ClaudeAgentContentBlock> DispatchToolAsync(
        ClaudeAgentContentBlock toolUse,
        AgentState state,
        CancellationToken ct)
    {
        var id = toolUse.Id!;
        return toolUse.Name switch
        {
            "get_recipe_ingredients" =>
                await ExecuteGetRecipeIngredients(id, Deserialize<GetRecipeIngredientsInput>(toolUse.Input!.Value), ct),
            "save_nutrition_analysis" =>
                ExecuteSaveNutritionAnalysis(id, Deserialize<SaveNutritionAnalysisInput>(toolUse.Input!.Value), state),
            _ =>
                // MCP-proxied tool: forward to nutrition MCP server
                await ExecuteMcpToolAsync(id, toolUse.Name!, toolUse.Input!.Value, ct),
        };
    }

    // ── Tool implementations ───────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> ExecuteGetRecipeIngredients(
        string toolUseId,
        GetRecipeIngredientsInput input,
        CancellationToken ct)
    {
        if (!Guid.TryParse(input.RecipeId, out var guid))
            return ErrorResult(toolUseId, $"Invalid recipe ID: {input.RecipeId}");

        var recipe = await _recipeRepository.GetByIdAsync(RecipeId.From(guid), ct);
        if (recipe is null)
            return ErrorResult(toolUseId, $"Recipe '{input.RecipeId}' not found.");

        var result = new
        {
            recipeId    = input.RecipeId,
            recipeName  = recipe.Name.Value,
            ingredients = recipe.Ingredients
                .Select(i => new { name = i.Name, quantity = i.Quantity, unit = i.Unit })
                .ToList()
        };
        return OkResult(toolUseId, JsonSerializer.Serialize(result, JsonOptions));
    }

    private async Task<ClaudeAgentContentBlock> ExecuteMcpToolAsync(
        string toolUseId,
        string toolName,
        JsonElement input,
        CancellationToken ct)
    {
        try
        {
            var result = await _mcpClient.CallToolAsync(toolName, input, ct);
            return OkResult(toolUseId, result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MCP tool '{Tool}' failed: {Msg}", toolName, ex.Message);
            return ErrorResult(toolUseId, $"MCP tool '{toolName}' failed: {ex.Message}");
        }
    }

    private static ClaudeAgentContentBlock ExecuteSaveNutritionAnalysis(
        string toolUseId,
        SaveNutritionAnalysisInput input,
        AgentState state)
    {
        state.Result = new NutritionAnalysisDto(
            Guid.Parse(input.RecipeId),
            input.RecipeName,
            input.Ingredients.Select(i => new IngredientNutritionDto(
                i.Name, i.Quantity, i.Unit,
                i.Calories, i.ProteinG, i.CarbG, i.FatG, i.FiberG)).ToList(),
            new NutritionTotalsDto(
                input.Totals.Calories, input.Totals.ProteinG, input.Totals.CarbG,
                input.Totals.FatG, input.Totals.FiberG));
        state.LoopComplete = true;
        return OkResult(toolUseId, "Nutrition analysis saved.");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static T Deserialize<T>(JsonElement element)
        => JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions)!;

    private static ClaudeAgentContentBlock OkResult(string toolUseId, string content) =>
        new("tool_result", ToolUseId: toolUseId, Content: content);

    private static ClaudeAgentContentBlock ErrorResult(string toolUseId, string message) =>
        new("tool_result", ToolUseId: toolUseId, Content: message, IsError: true);

    private sealed class AgentState
    {
        public NutritionAnalysisDto? Result    { get; set; }
        public bool                  LoopComplete { get; set; }
    }
}
