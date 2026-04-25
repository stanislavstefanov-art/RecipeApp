# F2 — Agentic URL Import: Implementation Plan

Reference spec: `Docs/Specs/F2-agentic-url-import.md`

Build order matches dependency direction: models → tools → agent → application slice → endpoint → DI → docs → tests.

---

## Step 1 — Claude tool-use API models

**File:** `Backend/src/Recipes.Infrastructure/AI/Claude/Models/ClaudeToolModels.cs`

The existing `ClaudeMessagesRequest` / `ClaudeMessagesResponse` handle text-only
conversations and must not be changed. The agent needs its own model layer that
supports `tools`, `tool_choice`, `tool_use` content blocks, and `tool_result` content
blocks.

```csharp
// ── Request side ───────────────────────────────────────────────────────────

// A "fat" content block covering every variant used by the agent.
// JsonIgnore(WhenWritingNull) means unused fields are omitted from JSON.
public sealed record ClaudeAgentContentBlock(
    [property: JsonPropertyName("type")]        string Type,
    [property: JsonPropertyName("text"),        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Text        = null,
    [property: JsonPropertyName("id"),          JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Id          = null,
    [property: JsonPropertyName("name"),        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Name        = null,
    [property: JsonPropertyName("input"),       JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonElement? Input       = null,
    [property: JsonPropertyName("tool_use_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      ToolUseId   = null,
    [property: JsonPropertyName("content"),     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string?      Content     = null,
    [property: JsonPropertyName("is_error"),    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool?        IsError     = null
);

public sealed record ClaudeAgentMessage(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] IReadOnlyList<ClaudeAgentContentBlock> Content
);

public sealed record ClaudeToolDefinition(
    [property: JsonPropertyName("name")]         string      Name,
    [property: JsonPropertyName("description")]  string      Description,
    [property: JsonPropertyName("input_schema")] JsonElement InputSchema
);

// tool_choice shapes
public sealed record ClaudeToolChoiceAuto(
    [property: JsonPropertyName("type")] string Type = "auto"
);

public sealed record ClaudeAgentRequest(
    [property: JsonPropertyName("model")]       string Model,
    [property: JsonPropertyName("max_tokens")]  int MaxTokens,
    [property: JsonPropertyName("system")]      string System,
    [property: JsonPropertyName("tools")]       IReadOnlyList<ClaudeToolDefinition> Tools,
    [property: JsonPropertyName("tool_choice")] object ToolChoice,
    [property: JsonPropertyName("messages")]    IReadOnlyList<ClaudeAgentMessage> Messages
);

// ── Response side ──────────────────────────────────────────────────────────

public sealed record ClaudeAgentResponse(
    [property: JsonPropertyName("id")]          string Id,
    [property: JsonPropertyName("stop_reason")] string StopReason,
    [property: JsonPropertyName("content")]     IReadOnlyList<ClaudeAgentContentBlock> Content,
    [property: JsonPropertyName("usage")]       ClaudeUsage? Usage
);

public sealed record ClaudeUsage(
    [property: JsonPropertyName("input_tokens")]                  int  InputTokens,
    [property: JsonPropertyName("output_tokens")]                 int  OutputTokens,
    [property: JsonPropertyName("cache_read_input_tokens")]       int? CacheReadInputTokens,
    [property: JsonPropertyName("cache_creation_input_tokens")]   int? CacheCreationInputTokens
);
```

`JsonElement` for `Input` (tool_use block from Claude) lets the agent deserialize each
tool's input into the appropriate C# type without a full polymorphic hierarchy.

---

## Step 2 — Tool input/output types

**File:** `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeImportToolTypes.cs`

Plain C# records used to deserialize tool call inputs and to build tool results.

```csharp
// fetch_url_content
internal sealed record FetchUrlInput(
    [property: JsonPropertyName("url")] string Url
);

// extract_recipe_fields
internal sealed record ExtractRecipeFieldsInput(
    [property: JsonPropertyName("title")]       string?                  Title,
    [property: JsonPropertyName("servings")]    int?                     Servings,
    [property: JsonPropertyName("ingredients")] List<RawIngredientItem>  Ingredients,
    [property: JsonPropertyName("steps")]       List<string>             Steps,
    [property: JsonPropertyName("notes")]       string?                  Notes
);

internal sealed record RawIngredientItem(
    [property: JsonPropertyName("name")]        string  Name,
    [property: JsonPropertyName("rawQuantity")] string? RawQuantity,
    [property: JsonPropertyName("rawUnit")]     string? RawUnit,
    [property: JsonPropertyName("notes")]       string? Notes
);

// normalize_ingredient
internal sealed record NormalizeIngredientInput(
    [property: JsonPropertyName("name")]        string  Name,
    [property: JsonPropertyName("rawQuantity")] string? RawQuantity,
    [property: JsonPropertyName("rawUnit")]     string? RawUnit
);

internal sealed record NormalizeIngredientResult(
    [property: JsonPropertyName("normalizedName")] string   NormalizedName,
    [property: JsonPropertyName("quantity")]        decimal? Quantity,
    [property: JsonPropertyName("unit")]            string?  Unit
);

// save_recipe
internal sealed record SaveRecipeInput(
    [property: JsonPropertyName("title")]       string?                       Title,
    [property: JsonPropertyName("servings")]    int?                          Servings,
    [property: JsonPropertyName("ingredients")] List<NormalizedIngredientItem> Ingredients,
    [property: JsonPropertyName("steps")]       List<string>                  Steps,
    [property: JsonPropertyName("notes")]       string?                       Notes,
    [property: JsonPropertyName("confidence")]  double                        Confidence,
    [property: JsonPropertyName("needsReview")] bool                          NeedsReview
);

internal sealed record NormalizedIngredientItem(
    [property: JsonPropertyName("name")]     string   Name,
    [property: JsonPropertyName("quantity")] decimal? Quantity,
    [property: JsonPropertyName("unit")]     string?  Unit,
    [property: JsonPropertyName("notes")]    string?  Notes
);
```

---

## Step 3 — Tool schemas

**File:** `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeImportTools.cs`

Static class holding `ClaudeToolDefinition` instances. Schemas are inlined as
`JsonDocument.Parse(...)` to keep definitions colocated with their descriptions.
Use `static readonly` at class level so they are parsed once.

```csharp
internal static class RecipeImportTools
{
    internal static readonly ClaudeToolDefinition FetchUrlContent = new(
        Name: "fetch_url_content",
        Description: """
            Fetch the HTML content from a URL and return it as cleaned plain text.
            Call this first to get the raw recipe page content.
            Returns { "content": "..." } on success.
            Returns is_error: true with { "error": "..." } on network failure or non-2xx response.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["url"],
              "additionalProperties": false,
              "properties": {
                "url": {
                  "type": "string",
                  "description": "Absolute http or https URL to fetch."
                }
              }
            }
            """).RootElement
    );

    internal static readonly ClaudeToolDefinition ExtractRecipeFields = new(
        Name: "extract_recipe_fields",
        Description: """
            Submit your initial extraction of recipe fields from the fetched text.
            Validates the structure and reports the number of ingredients to normalize.
            Returns { "ingredientCount": N, "message": "..." }.
            Returns is_error: true if ingredients or steps are missing.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["title", "servings", "ingredients", "steps"],
              "additionalProperties": false,
              "properties": {
                "title":    { "type": ["string", "null"] },
                "servings": { "type": ["integer", "null"], "minimum": 1 },
                "ingredients": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["name"],
                    "additionalProperties": false,
                    "properties": {
                      "name":        { "type": "string" },
                      "rawQuantity": { "type": ["string", "null"], "description": "Free-form quantity, e.g. '2½' or '3/4'." },
                      "rawUnit":     { "type": ["string", "null"], "description": "Free-form unit, e.g. 'tablespoons'." },
                      "notes":       { "type": ["string", "null"] }
                    }
                  }
                },
                "steps": { "type": "array", "items": { "type": "string" } },
                "notes": { "type": ["string", "null"] }
              }
            }
            """).RootElement
    );

    internal static readonly ClaudeToolDefinition NormalizeIngredient = new(
        Name: "normalize_ingredient",
        Description: """
            Normalize a single ingredient's name, quantity, and unit into canonical form.
            Call this in parallel for every ingredient returned by extract_recipe_fields.
            Returns { "normalizedName": "...", "quantity": 2.5, "unit": "tbsp" }.
            Never errors — unknown units pass through unchanged.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["name"],
              "additionalProperties": false,
              "properties": {
                "name":        { "type": "string",          "description": "Ingredient name as extracted." },
                "rawQuantity": { "type": ["string", "null"], "description": "Free-form quantity text." },
                "rawUnit":     { "type": ["string", "null"], "description": "Free-form unit text." }
              }
            }
            """).RootElement
    );

    internal static readonly ClaudeToolDefinition SaveRecipe = new(
        Name: "save_recipe",
        Description: """
            Submit the final recipe with all normalized ingredients. This is the last
            tool call — invoke it once all ingredients are normalized.
            Returns { "status": "saved" }.
            """,
        InputSchema: JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["title", "ingredients", "steps", "confidence", "needsReview"],
              "additionalProperties": false,
              "properties": {
                "title":    { "type": ["string", "null"] },
                "servings": { "type": ["integer", "null"], "minimum": 1 },
                "ingredients": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "required": ["name"],
                    "additionalProperties": false,
                    "properties": {
                      "name":     { "type": "string" },
                      "quantity": { "type": ["number", "null"] },
                      "unit":     { "type": ["string", "null"] },
                      "notes":    { "type": ["string", "null"] }
                    }
                  }
                },
                "steps":       { "type": "array", "items": { "type": "string" } },
                "notes":       { "type": ["string", "null"] },
                "confidence":  { "type": "number", "minimum": 0, "maximum": 1 },
                "needsReview": { "type": "boolean" }
              }
            }
            """).RootElement
    );

    internal static readonly IReadOnlyList<ClaudeToolDefinition> All =
        [FetchUrlContent, ExtractRecipeFields, NormalizeIngredient, SaveRecipe];
}
```

---

## Step 4 — Application interface

**File:** `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/IRecipeImportAgent.cs`

```csharp
using ErrorOr;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public interface IRecipeImportAgent
{
    Task<ErrorOr<ImportedRecipeDto>> RunAsync(string sourceUrl, CancellationToken cancellationToken);
}
```

---

## Step 5 — Application slice

### Command
**File:** `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/ImportRecipeFromUrlCommand.cs`

```csharp
using ErrorOr;
using MediatR;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public sealed record ImportRecipeFromUrlCommand(string SourceUrl)
    : IRequest<ErrorOr<ImportedRecipeDto>>;
```

### Validator
**File:** `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/ImportRecipeFromUrlValidator.cs`

```csharp
using FluentValidation;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public sealed class ImportRecipeFromUrlValidator : AbstractValidator<ImportRecipeFromUrlCommand>
{
    public ImportRecipeFromUrlValidator()
    {
        RuleFor(x => x.SourceUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                         && u.Scheme is "http" or "https")
            .WithMessage("SourceUrl must be an absolute http or https URL.");
    }
}
```

### Handler
**File:** `Backend/src/Recipes.Application/Recipes/ImportRecipeFromUrl/ImportRecipeFromUrlHandler.cs`

```csharp
using ErrorOr;
using MediatR;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public sealed class ImportRecipeFromUrlHandler
    : IRequestHandler<ImportRecipeFromUrlCommand, ErrorOr<ImportedRecipeDto>>
{
    private readonly IRecipeImportAgent _agent;

    public ImportRecipeFromUrlHandler(IRecipeImportAgent agent) => _agent = agent;

    public Task<ErrorOr<ImportedRecipeDto>> Handle(
        ImportRecipeFromUrlCommand request,
        CancellationToken cancellationToken)
        => _agent.RunAsync(request.SourceUrl, cancellationToken);
}
```

---

## Step 6 — RecipeImportAgent

**File:** `Backend/src/Recipes.Infrastructure/AI/Claude/Agents/RecipeImportAgent.cs`

### 6a — Private helpers (inner implementations)

#### SSRF guard
```csharp
private static bool IsPrivateAddress(Uri uri)
{
    try
    {
        var addresses = Dns.GetHostAddresses(uri.Host);
        return addresses.Any(IsPrivateIp);
    }
    catch { return false; }
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
```

#### HTML stripper
```csharp
private static string StripHtml(string html)
{
    html = Regex.Replace(html,
        @"<(script|style)[^>]*>.*?</(script|style)>",
        " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    html = Regex.Replace(html, @"<[^>]+>", " ");
    html = WebUtility.HtmlDecode(html);
    html = Regex.Replace(html, @"\s{2,}", " ").Trim();
    return html.Length > 50_000 ? html[..50_000] : html;
}
```

#### Fraction / quantity parser
```csharp
private static decimal? ParseQuantity(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    raw = raw.Trim()
             .Replace("½", "1/2").Replace("¼", "1/4").Replace("¾", "3/4")
             .Replace("⅓", "1/3").Replace("⅔", "2/3").Replace("⅛", "1/8")
             .Replace("⅜", "3/8").Replace("⅝", "5/8").Replace("⅞", "7/8");

    var mixed = Regex.Match(raw, @"^(\d+)\s+(\d+)/(\d+)$");
    if (mixed.Success)
    {
        var whole = decimal.Parse(mixed.Groups[1].Value);
        var num   = decimal.Parse(mixed.Groups[2].Value);
        var den   = decimal.Parse(mixed.Groups[3].Value);
        return den == 0 ? null : whole + num / den;
    }

    var frac = Regex.Match(raw, @"^(\d+)/(\d+)$");
    if (frac.Success)
    {
        var num = decimal.Parse(frac.Groups[1].Value);
        var den = decimal.Parse(frac.Groups[2].Value);
        return den == 0 ? null : num / den;
    }

    return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
}
```

#### Unit lookup table
```csharp
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
```

### 6b — Tool implementations

#### fetch_url_content
```csharp
private async Task<ClaudeAgentContentBlock> ExecuteFetchUrlAsync(
    string toolUseId, FetchUrlInput input, CancellationToken ct)
{
    if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var uri)
        || uri.Scheme is not ("http" or "https"))
        return ErrorResult(toolUseId, "Invalid URL — must be absolute http or https.");

    if (IsPrivateAddress(uri))
        return ErrorResult(toolUseId, "URL resolves to a private/loopback address.");

    try
    {
        using var response = await _urlFetcher.GetAsync(input.Url, ct);
        if (!response.IsSuccessStatusCode)
            return ErrorResult(toolUseId, $"HTTP {(int)response.StatusCode} from {input.Url}");

        var html = await response.Content.ReadAsStringAsync(ct);
        var text = StripHtml(html);

        if (text.Length < 100)
            return ErrorResult(toolUseId, "Page content too short to contain a recipe.");

        return OkResult(toolUseId, JsonSerializer.Serialize(new { content = text }, _json));
    }
    catch (Exception ex)
    {
        return ErrorResult(toolUseId, $"Fetch failed: {ex.Message}");
    }
}
```

#### extract_recipe_fields
```csharp
private ClaudeAgentContentBlock ExecuteExtractRecipeFields(
    string toolUseId, ExtractRecipeFieldsInput input, AgentState state)
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
    }, _json));
}
```

#### normalize_ingredient
```csharp
private ClaudeAgentContentBlock ExecuteNormalizeIngredient(
    string toolUseId, NormalizeIngredientInput input)
{
    var normalized = new NormalizeIngredientResult(
        NormalizedName: input.Name.Trim().ToLowerInvariant(),
        Quantity:       ParseQuantity(input.RawQuantity),
        Unit:           input.RawUnit is null ? null
                        : UnitMap.TryGetValue(input.RawUnit.Trim(), out var canon) ? canon
                        : input.RawUnit.Trim()
    );
    return OkResult(toolUseId, JsonSerializer.Serialize(normalized, _json));
}
```

#### save_recipe (terminal)
```csharp
private ClaudeAgentContentBlock ExecuteSaveRecipe(
    string toolUseId, SaveRecipeInput input, AgentState state)
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
```

#### Helper result factories
```csharp
private static ClaudeAgentContentBlock OkResult(string toolUseId, string content) =>
    new(Type: "tool_result", ToolUseId: toolUseId, Content: content);

private static ClaudeAgentContentBlock ErrorResult(string toolUseId, string message) =>
    new(Type: "tool_result", ToolUseId: toolUseId, Content: message, IsError: true);
```

### 6c — Tool dispatcher
```csharp
private async Task<ClaudeAgentContentBlock> DispatchToolAsync(
    ClaudeAgentContentBlock toolUse, AgentState state, CancellationToken ct)
{
    var id = toolUse.Id!;
    return toolUse.Name switch
    {
        "fetch_url_content"    => await ExecuteFetchUrlAsync(id,
                                        Deserialize<FetchUrlInput>(toolUse.Input!.Value), ct),
        "extract_recipe_fields" => ExecuteExtractRecipeFields(id,
                                        Deserialize<ExtractRecipeFieldsInput>(toolUse.Input!.Value), state),
        "normalize_ingredient" => ExecuteNormalizeIngredient(id,
                                        Deserialize<NormalizeIngredientInput>(toolUse.Input!.Value)),
        "save_recipe"          => ExecuteSaveRecipe(id,
                                        Deserialize<SaveRecipeInput>(toolUse.Input!.Value), state),
        _                      => ErrorResult(id, $"Unknown tool: {toolUse.Name}")
    };
}

private T Deserialize<T>(JsonElement element) =>
    JsonSerializer.Deserialize<T>(element.GetRawText(), _json)
    ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
```

### 6d — Main loop
```csharp
public async Task<ErrorOr<ImportedRecipeDto>> RunAsync(string sourceUrl, CancellationToken ct)
{
    const int MaxIterations = 10;
    const string SystemPrompt = """
        You are a recipe extraction agent. Use your tools to fetch the URL, extract
        recipe fields from the text, normalize each ingredient in parallel, then call
        save_recipe with the final result.

        If fetching fails, retry once. If it fails again, call save_recipe with
        needsReview: true and your best effort from any partial content.
        """;

    var messages = new List<ClaudeAgentMessage>
    {
        new("user", [new ClaudeAgentContentBlock("text", Text: $"Extract the recipe from: {sourceUrl}")])
    };

    var state = new AgentState();

    for (int i = 0; i < MaxIterations; i++)
    {
        _logger.LogInformation("RecipeImportAgent iteration {Iteration}", i + 1);

        var response = await CallClaudeAsync(messages, SystemPrompt, ct);

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

        // Execute all tool calls from this turn in parallel
        var results = await Task.WhenAll(
            toolUseBlocks.Select(b => DispatchToolAsync(b, state, ct)));

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
```

### 6e — Claude HTTP call
```csharp
private async Task<ClaudeAgentResponse> CallClaudeAsync(
    IReadOnlyList<ClaudeAgentMessage> messages,
    string system,
    CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(_options.ApiKey))
        throw new InvalidOperationException("Claude API key is missing.");

    var request = new ClaudeAgentRequest(
        Model:      _options.Model,
        MaxTokens:  4096,
        System:     system,
        Tools:      RecipeImportTools.All,
        ToolChoice: new ClaudeToolChoiceAuto(),
        Messages:   messages
    );

    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
    httpRequest.Headers.Add("x-api-key", _options.ApiKey);
    httpRequest.Headers.Add("anthropic-version", "2023-06-01");
    httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    httpRequest.Content = new StringContent(
        JsonSerializer.Serialize(request, _json), Encoding.UTF8, "application/json");

    using var response = await _claudeClient.SendAsync(httpRequest, ct);
    var body = await response.Content.ReadAsStringAsync(ct);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogError("Claude API error {Status}: {Body}", (int)response.StatusCode, body);
        throw new InvalidOperationException($"Claude API error {(int)response.StatusCode}: {body}");
    }

    return JsonSerializer.Deserialize<ClaudeAgentResponse>(body, _json)
           ?? throw new InvalidOperationException("Empty Claude response.");
}
```

### 6f — Constructor and fields
```csharp
public sealed class RecipeImportAgent : IRecipeImportAgent
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
}

// Mutable state scoped to one loop run
private sealed class AgentState
{
    public ExtractRecipeFieldsInput? Draft { get; set; }
    public ImportedRecipeDto?       Result { get; set; }
    public bool                     LoopComplete { get; set; }
}
```

---

## Step 7 — DI registration

**File:** `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` — add inside `AddInfrastructure`:

```csharp
// Named HttpClient for Claude agentic calls (60 s — loops can be long)
services.AddHttpClient("ClaudeAgent", client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});

// Named HttpClient for URL fetching by the import agent (15 s per fetch)
services.AddHttpClient("RecipeUrlFetcher", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("RecipeApp/1.0");
});

services.AddScoped<IRecipeImportAgent, RecipeImportAgent>();
```

Also add the `ImportRecipeFromUrlValidator` to the FluentValidation scan — check how
existing validators are registered. If they use `AssemblyScanner` or
`AddValidatorsFromAssembly`, no change is needed; the new validator is picked up
automatically.

---

## Step 8 — Endpoint

**File:** `Backend/src/Recipes.Api/Endpoints/RecipesEndpoints.cs`

Add after the existing `/import` route inside `MapRecipesEndpoints`:

```csharp
group.MapPost("/import/url", async (
    ImportRecipeFromUrlRequest request,
    ISender sender,
    CancellationToken ct) =>
{
    var result = await sender.Send(new ImportRecipeFromUrlCommand(request.SourceUrl), ct);
    return result.ToHttpResult(imported => Results.Ok(imported));
});
```

Add at the bottom of the file alongside the other request records:

```csharp
public sealed record ImportRecipeFromUrlRequest(string SourceUrl);
```

Add the `using` for the new namespace:
```csharp
using Recipes.Application.Recipes.ImportRecipeFromUrl;
```

---

## Step 9 — CCAF documentation

**File:** `Backend/Docs/CCAF/F2-agentic-url-import.md`

```markdown
# F2 — Agentic URL Import: CCAF Coverage

## What this implements
A multi-turn Claude agentic loop that fetches a recipe webpage, extracts structured
fields, normalizes each ingredient, and returns an ImportedRecipeDto — all via
tool_use rather than JSON-in-prompt.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| Tool schema (`input_schema`) design | `RecipeImportTools.cs` — four JsonDocument schemas |
| Tool description writing | Each `ClaudeToolDefinition.Description` field |
| Multi-turn loop (`stop_reason: tool_use`) | `RecipeImportAgent.RunAsync` for loop |
| Parallel tool calls | `Task.WhenAll(toolUseBlocks.Select(DispatchToolAsync))` |
| `is_error: true` tool error handling | `fetch_url_content` error path; agent retries |
| Structured output via terminal tool | `save_recipe` input is the final ImportedRecipeDto |
| `tool_choice: auto` | `ClaudeAgentRequest.ToolChoice = new ClaudeToolChoiceAuto()` |

## Key decisions

**Why `tool_choice: auto` instead of forced?**
`auto` lets Claude decide the calling sequence, demonstrating the real agentic pattern
where the model orchestrates multi-step work. Forced choice (`{type:"tool",name:"..."}`)
is a one-shot structured-output technique covered by F4's prompts primitive.

**Why is `normalize_ingredient` a tool rather than done in-process before `save_recipe`?**
It demonstrates parallel tool calls — Claude emits one `tool_use` block per ingredient
in a single response, all executed concurrently on the backend. If normalization were
invisible to Claude, the parallel pattern wouldn't be exercised.

**Why does `extract_recipe_fields` exist as a separate step from `save_recipe`?**
It forces an intermediate turn where Claude submits a draft and the backend responds
before normalization happens. This makes the multi-turn nature visible in logs and
tests, and gives the backend a checkpoint to validate before expensive normalization.

**Comparison to the existing JSON-in-prompt import**
The existing `ClaudeRecipeImportClient` sends one request and parses JSON from a text
block. This approach: (a) eliminates JSON-in-text fragility, (b) enables parallel
normalization, (c) gives Claude error feedback per tool rather than a monolithic retry,
(d) produces an audit trail of every step via tool call logs.
```

---

## Step 10 — Unit tests

**File:** `Backend/tests/Recipes.Infrastructure.Tests/AI/RecipeImportAgentTests.cs`

Use `WebApplicationFactory` or a plain unit test with a fake `IHttpClientFactory` that
returns a scripted `MockHttpMessageHandler`.

### Test cases

**T1 — Happy path**
Script: turn 1 → `fetch_url_content`; turn 2 → `extract_recipe_fields` + two `normalize_ingredient`; turn 3 → `save_recipe`.
Assert: returns `Ok<ImportedRecipeDto>` with normalized ingredient names (lowercase) and parsed quantity.

**T2 — Fetch failure → retry → save**
Script: turn 1 → `fetch_url_content` returns `is_error: true`; turn 2 → `fetch_url_content` again (success); continues to save.
Assert: returns `Ok<ImportedRecipeDto>`.

**T3 — Two fetch failures → save with needsReview**
Script: two consecutive `fetch_url_content` → `is_error: true`; then `save_recipe` with `needsReview: true`.
Assert: returns `Ok<ImportedRecipeDto>` where `NeedsReview == true`.

**T4 — Loop never calls save_recipe**
Script: fake Claude always returns `stop_reason: tool_use` calling `fetch_url_content` indefinitely.
Assert: after 10 iterations returns `IsError == true`.

**T5 — ParseQuantity handles fractions**
Unit test directly on the private-via-reflection or extracted-static helper:
- `"3/4"` → `0.75m`
- `"1 1/2"` → `1.5m`
- `"½"` → `0.5m`
- `"2"` → `2m`
- `"abc"` → `null`

**T6 — Unit normalization**
- `"tablespoons"` → `"tbsp"`
- `"Grams"` → `"g"` (case-insensitive)
- `"handful"` → `"handful"` (unknown, pass-through)
- `null` → `null`

---

## Implementation checklist

- [ ] Step 1 — `ClaudeToolModels.cs`
- [ ] Step 2 — `RecipeImportToolTypes.cs`
- [ ] Step 3 — `RecipeImportTools.cs`
- [ ] Step 4 — `IRecipeImportAgent.cs`
- [ ] Step 5 — Command / Validator / Handler
- [ ] Step 6 — `RecipeImportAgent.cs`
- [ ] Step 7 — DI registrations
- [ ] Step 8 — Endpoint + request record
- [ ] Step 9 — CCAF docs
- [ ] Step 10 — Unit tests
- [ ] `dotnet build Backend/Recipes.sln` passes
- [ ] `dotnet test Backend/Recipes.sln` passes
- [ ] Smoke test: POST real recipe URL, confirm `ImportedRecipeDto` returned
