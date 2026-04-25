using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Application.Recipes.ImportRecipeFromUrl;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Agents;

public sealed class RecipeImportAgent : IRecipeImportAgent
{
    private const int MaxIterations = 10;

    private const string SystemPrompt = """
        You are a recipe extraction agent. Use your tools to fetch the URL, extract
        recipe fields from the text, normalize each ingredient in parallel, then call
        save_recipe with the final result.

        Work step by step:
        1. Fetch the URL with fetch_url_content.
        2. Call extract_recipe_fields with the title, servings, raw ingredients, and steps.
        3. Call normalize_ingredient for every ingredient — all in the same response to run in parallel.
        4. Call save_recipe with the fully normalized recipe.

        If fetching fails, retry once. If it fails again, call save_recipe with
        needsReview: true and your best effort from any partial content.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Dictionary<string, string> UnitMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["tablespoon"] = "tbsp", ["tablespoons"] = "tbsp", ["tbsp"] = "tbsp", ["tbs"] = "tbsp",
            ["teaspoon"]   = "tsp",  ["teaspoons"]   = "tsp",  ["tsp"]  = "tsp",
            ["cup"]        = "cup",  ["cups"]         = "cup",
            ["gram"]       = "g",    ["grams"]        = "g",    ["g"]    = "g",
            ["kilogram"]   = "kg",   ["kilograms"]    = "kg",   ["kg"]   = "kg",
            ["ounce"]      = "oz",   ["ounces"]       = "oz",   ["oz"]   = "oz",
            ["pound"]      = "lb",   ["pounds"]       = "lb",   ["lb"]   = "lb",  ["lbs"] = "lb",
            ["milliliter"] = "ml",   ["milliliters"]  = "ml",   ["ml"]   = "ml",
            ["liter"]      = "l",    ["liters"]       = "l",    ["l"]    = "l",
            ["pinch"]      = "pinch",["pinches"]      = "pinch",
            ["clove"]      = "clove",["cloves"]       = "clove",
            ["slice"]      = "slice",["slices"]       = "slice",
        };

    private readonly HttpClient _claudeClient;
    private readonly HttpClient _urlFetcher;
    private readonly ClaudeOptions _options;
    private readonly ILogger<RecipeImportAgent> _logger;

    public RecipeImportAgent(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        ILogger<RecipeImportAgent> logger)
    {
        _claudeClient = httpClientFactory.CreateClient("ClaudeAgent");
        _urlFetcher   = httpClientFactory.CreateClient("RecipeUrlFetcher");
        _options      = options.Value;
        _logger       = logger;
    }

    public async Task<ErrorOr<ImportedRecipeDto>> RunAsync(
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        var messages = new List<ClaudeAgentMessage>
        {
            new("user", [new ClaudeAgentContentBlock("text", Text: $"Extract the recipe from: {sourceUrl}")])
        };

        var state = new AgentState();

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            _logger.LogInformation("RecipeImportAgent iteration {Iteration}", iteration + 1);

            var response = await CallClaudeAsync(messages, cancellationToken);

            _logger.LogInformation(
                "Claude response: StopReason={StopReason}, InputTokens={In}, OutputTokens={Out}",
                response.StopReason,
                response.Usage?.InputTokens,
                response.Usage?.OutputTokens);

            messages.Add(new("assistant", response.Content));

            if (response.StopReason != "tool_use")
            {
                return state.Result is not null
                    ? state.Result
                    : Error.Failure(description: "Agent loop ended without calling save_recipe.");
            }

            var toolUseBlocks = response.Content
                .Where(b => b.Type == "tool_use")
                .ToList();

            var results = await Task.WhenAll(
                toolUseBlocks.Select(b => DispatchToolAsync(b, state, cancellationToken)));

            _logger.LogInformation(
                "Executed {Count} tool call(s): {Names}",
                toolUseBlocks.Count,
                string.Join(", ", toolUseBlocks.Select(b => b.Name)));

            if (state.LoopComplete)
                return state.Result!;

            messages.Add(new("user", results));
        }

        return Error.Failure(description: "Recipe import agent exceeded maximum iterations.");
    }

    // ── Claude HTTP call ───────────────────────────────────────────────────

    private async Task<ClaudeAgentResponse> CallClaudeAsync(
        IReadOnlyList<ClaudeAgentMessage> messages,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Claude API key is missing.");

        var request = new ClaudeAgentRequest(
            Model:      _options.Model,
            MaxTokens:  4096,
            System:     SystemPrompt,
            Tools:      RecipeImportTools.All,
            ToolChoice: new ClaudeToolChoiceAuto(),
            Messages:   messages
        );

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _claudeClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Claude API error {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"Claude API error {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<ClaudeAgentResponse>(body, JsonOptions)
               ?? throw new InvalidOperationException("Empty Claude response.");
    }

    // ── Tool dispatcher ────────────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> DispatchToolAsync(
        ClaudeAgentContentBlock toolUse,
        AgentState state,
        CancellationToken cancellationToken)
    {
        var id = toolUse.Id!;
        return toolUse.Name switch
        {
            "fetch_url_content"     => await ExecuteFetchUrlAsync(
                                           id, Deserialize<FetchUrlInput>(toolUse.Input!.Value), cancellationToken),
            "extract_recipe_fields" => ExecuteExtractRecipeFields(
                                           id, Deserialize<ExtractRecipeFieldsInput>(toolUse.Input!.Value), state),
            "normalize_ingredient"  => ExecuteNormalizeIngredient(
                                           id, Deserialize<NormalizeIngredientInput>(toolUse.Input!.Value)),
            "save_recipe"           => ExecuteSaveRecipe(
                                           id, Deserialize<SaveRecipeInput>(toolUse.Input!.Value), state),
            _                       => ErrorResult(id, $"Unknown tool: {toolUse.Name}")
        };
    }

    // ── Tool implementations ───────────────────────────────────────────────

    private async Task<ClaudeAgentContentBlock> ExecuteFetchUrlAsync(
        string toolUseId,
        FetchUrlInput input,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
            return ErrorResult(toolUseId, "Invalid URL — must be absolute http or https.");

        if (IsPrivateAddress(uri))
            return ErrorResult(toolUseId, "URL resolves to a private or loopback address.");

        try
        {
            using var response = await _urlFetcher.GetAsync(input.Url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ErrorResult(toolUseId, $"HTTP {(int)response.StatusCode} from {input.Url}");

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var text = StripHtml(html);

            if (text.Length < 100)
                return ErrorResult(toolUseId, "Page content too short to contain a recipe.");

            return OkResult(toolUseId, JsonSerializer.Serialize(new { content = text }, JsonOptions));
        }
        catch (Exception ex)
        {
            return ErrorResult(toolUseId, $"Fetch failed: {ex.Message}");
        }
    }

    private ClaudeAgentContentBlock ExecuteExtractRecipeFields(
        string toolUseId,
        ExtractRecipeFieldsInput input,
        AgentState state)
    {
        if (input.Ingredients.Count == 0)
            return ErrorResult(toolUseId, "ingredients must not be empty.");

        if (input.Steps.Count == 0)
            return ErrorResult(toolUseId, "steps must not be empty.");

        state.Draft = input;

        return OkResult(toolUseId, JsonSerializer.Serialize(new
        {
            ingredientCount = input.Ingredients.Count,
            message = $"Draft received. Now call normalize_ingredient for each of the {input.Ingredients.Count} ingredients."
        }, JsonOptions));
    }

    private ClaudeAgentContentBlock ExecuteNormalizeIngredient(
        string toolUseId,
        NormalizeIngredientInput input)
    {
        var result = new NormalizeIngredientResult(
            NormalizedName: input.Name.Trim().ToLowerInvariant(),
            Quantity:       ParseQuantity(input.RawQuantity),
            Unit:           input.RawUnit is null ? null
                            : UnitMap.TryGetValue(input.RawUnit.Trim(), out var canon)
                                ? canon
                                : input.RawUnit.Trim()
        );
        return OkResult(toolUseId, JsonSerializer.Serialize(result, JsonOptions));
    }

    private ClaudeAgentContentBlock ExecuteSaveRecipe(
        string toolUseId,
        SaveRecipeInput input,
        AgentState state)
    {
        state.Result = new ImportedRecipeDto(
            Title:       input.Title,
            Servings:    input.Servings,
            Ingredients: input.Ingredients
                .Select(i => new ImportedIngredientDto(i.Name, i.Quantity, i.Unit, i.Notes))
                .ToList(),
            Steps:       input.Steps,
            Notes:       input.Notes,
            Confidence:  input.Confidence,
            NeedsReview: input.NeedsReview
        );
        state.LoopComplete = true;
        return OkResult(toolUseId, """{"status":"saved"}""");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static ClaudeAgentContentBlock OkResult(string toolUseId, string content) =>
        new(Type: "tool_result", ToolUseId: toolUseId, Content: content);

    private static ClaudeAgentContentBlock ErrorResult(string toolUseId, string message) =>
        new(Type: "tool_result", ToolUseId: toolUseId, Content: message, IsError: true);

    private static T Deserialize<T>(JsonElement element) =>
        JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions)
        ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");

    private static string StripHtml(string html)
    {
        html = Regex.Replace(
            html,
            @"<(script|style)[^>]*>.*?</(script|style)>",
            " ",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", " ");
        html = WebUtility.HtmlDecode(html);
        html = Regex.Replace(html, @"\s{2,}", " ").Trim();
        return html.Length > 50_000 ? html[..50_000] : html;
    }

    internal static decimal? ParseQuantity(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        raw = raw.Trim()
                 .Replace("½", "1/2").Replace("¼", "1/4").Replace("¾", "3/4")
                 .Replace("⅓", "1/3").Replace("⅔", "2/3").Replace("⅛", "1/8")
                 .Replace("⅜", "3/8").Replace("⅝", "5/8").Replace("⅞", "7/8");

        var mixed = Regex.Match(raw, @"^(\d+)\s+(\d+)/(\d+)$");
        if (mixed.Success)
        {
            var whole = decimal.Parse(mixed.Groups[1].Value, CultureInfo.InvariantCulture);
            var num   = decimal.Parse(mixed.Groups[2].Value, CultureInfo.InvariantCulture);
            var den   = decimal.Parse(mixed.Groups[3].Value, CultureInfo.InvariantCulture);
            return den == 0 ? null : whole + num / den;
        }

        var frac = Regex.Match(raw, @"^(\d+)/(\d+)$");
        if (frac.Success)
        {
            var num = decimal.Parse(frac.Groups[1].Value, CultureInfo.InvariantCulture);
            var den = decimal.Parse(frac.Groups[2].Value, CultureInfo.InvariantCulture);
            return den == 0 ? null : num / den;
        }

        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static bool IsPrivateAddress(Uri uri)
    {
        try
        {
            var addresses = Dns.GetHostAddresses(uri.Host);
            return addresses.Any(IsPrivateIp);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPrivateIp(IPAddress addr)
    {
        if (addr.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = addr.GetAddressBytes();
        return b[0] == 127
            || b[0] == 10
            || (b[0] == 172 && b[1] is >= 16 and <= 31)
            || (b[0] == 192 && b[1] == 168);
    }

    // ── Loop state ─────────────────────────────────────────────────────────

    private sealed class AgentState
    {
        public ExtractRecipeFieldsInput? Draft        { get; set; }
        public ImportedRecipeDto?        Result       { get; set; }
        public bool                      LoopComplete { get; set; }
    }
}
