# F8 — Tool-Call Observability: Implementation Plan

Reference spec: `Docs/specs/F8-tool-observability.md`

Build order: interface → implementation → DI → query/handler → endpoint → agent instrumentation → test fix → docs.

---

## Step 1 — Define `IToolCallTelemetry` and `ToolCallRecord`

Create `Backend/src/Recipes.Application/Common/AI/IToolCallTelemetry.cs`:

```csharp
namespace Recipes.Application.Common.AI;

public interface IToolCallTelemetry
{
    void Record(ToolCallRecord record);
    IReadOnlyList<ToolCallRecord> GetRecent(int limit);
}

public sealed record ToolCallRecord
{
    public Guid     Id                  { get; init; } = Guid.NewGuid();
    public DateTime Timestamp           { get; init; } = DateTime.UtcNow;
    public string   AgentName           { get; init; } = "";
    public string   ToolName            { get; init; } = "";
    public int      InputBytes          { get; init; }
    public int      OutputBytes         { get; init; }
    public long     LatencyMs           { get; init; }
    public bool     IsError             { get; init; }
    public int?     InputTokens         { get; init; }
    public int?     OutputTokens        { get; init; }
    public int?     CacheReadTokens     { get; init; }
    public int?     CacheCreationTokens { get; init; }
}
```

Place in `Application` (not `Infrastructure`) so both the query handler and the
Infrastructure implementations can reference the interface without breaking the
dependency direction.

---

## Step 2 — Implement `InMemoryToolCallTelemetry`

Create `Backend/src/Recipes.Infrastructure/Telemetry/InMemoryToolCallTelemetry.cs`:

```csharp
using System.Collections.Concurrent;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.Telemetry;

public sealed class InMemoryToolCallTelemetry : IToolCallTelemetry
{
    private const int MaxCapacity = 1_000;
    private readonly ConcurrentQueue<ToolCallRecord> _records = new();

    public void Record(ToolCallRecord record)
    {
        _records.Enqueue(record);
        while (_records.Count > MaxCapacity)
            _records.TryDequeue(out _);
    }

    public IReadOnlyList<ToolCallRecord> GetRecent(int limit) =>
        _records.TakeLast(Math.Min(limit, MaxCapacity)).ToList();
}
```

`ConcurrentQueue` is lock-free for enqueue/dequeue — safe across parallel tool
dispatches within one request and across concurrent requests.

---

## Step 3 — Register as singleton

In `DependencyInjection.cs`, add before the agent registrations:

```csharp
services.AddSingleton<IToolCallTelemetry, InMemoryToolCallTelemetry>();
```

Singleton is required: scoped would create a fresh instance per request, losing all
records after each HTTP call.

---

## Step 4 — MediatR query + handler

Create `Backend/src/Recipes.Application/Admin/GetToolCallsQuery.cs`:

```csharp
public sealed record GetToolCallsQuery(int Limit = 100) : IRequest<IReadOnlyList<ToolCallDto>>;

public sealed record ToolCallDto(Guid Id, DateTime Timestamp, string AgentName,
    string ToolName, int InputBytes, int OutputBytes, long LatencyMs, bool IsError,
    int? InputTokens, int? OutputTokens, int? CacheReadTokens, int? CacheCreationTokens);

public sealed class GetToolCallsHandler(IToolCallTelemetry telemetry)
    : IRequestHandler<GetToolCallsQuery, IReadOnlyList<ToolCallDto>>
{
    public Task<IReadOnlyList<ToolCallDto>> Handle(
        GetToolCallsQuery request, CancellationToken cancellationToken)
    {
        var records = telemetry.GetRecent(Math.Clamp(request.Limit, 1, 1_000));
        IReadOnlyList<ToolCallDto> result = records.Select(r => new ToolCallDto(
            r.Id, r.Timestamp, r.AgentName, r.ToolName,
            r.InputBytes, r.OutputBytes, r.LatencyMs, r.IsError,
            r.InputTokens, r.OutputTokens, r.CacheReadTokens, r.CacheCreationTokens))
            .ToList();
        return Task.FromResult(result);
    }
}
```

---

## Step 5 — Admin endpoint

Create `Backend/src/Recipes.Api/Endpoints/AdminEndpoints.cs`:

```csharp
public static class AdminEndpoints
{
    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").WithTags("Admin");
        group.MapGet("/tool-calls", async (int limit = 100, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetToolCallsQuery(limit), ct)));
        return app;
    }
}
```

Add `app.MapAdminEndpoints();` to `Program.cs`.

---

## Step 6 — Instrument agents

For both `RecipeImportAgent` and `NutritionAnalysisAgent`:

1. Add `IToolCallTelemetry _telemetry` field + constructor parameter.
2. Add `InstrumentedDispatchAsync` private method:

```csharp
private async Task<ClaudeAgentContentBlock> InstrumentedDispatchAsync(
    ClaudeAgentContentBlock toolUse, AgentState state, ClaudeUsage? usage, CancellationToken ct)
{
    var sw = Stopwatch.StartNew();
    var result = await DispatchToolAsync(toolUse, state, ct);
    sw.Stop();
    _telemetry.Record(new ToolCallRecord
    {
        AgentName           = "RecipeImportAgent",   // or "NutritionAnalysisAgent"
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
```

3. Replace `DispatchToolAsync` in the `Task.WhenAll` call with `InstrumentedDispatchAsync`,
   passing `response.Usage` from the current iteration.

`DispatchToolAsync` itself is unchanged.

---

## Step 7 — Fix unit test

`RecipeImportAgentTests` constructs `RecipeImportAgent` directly. Add a `NoOpTelemetry`
file-scoped class and pass it as the new fourth argument:

```csharp
file sealed class NoOpTelemetry : IToolCallTelemetry
{
    public void Record(ToolCallRecord record) { }
    public IReadOnlyList<ToolCallRecord> GetRecent(int limit) => [];
}
```

---

## Step 8 — CCAF doc

Create `Backend/Docs/CCAF/F8-tool-observability.md` covering the observability,
token-accounting, cost-awareness, and latency subtopics.
