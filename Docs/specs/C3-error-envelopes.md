# C3 — Error Envelopes

## Summary

AI service calls in the command handlers (`CritiqueRecipeHandler`,
`ScaleRecipeHandler`, `ReviewRecipeDraftHandler`, `SubmitRecipeBatchAnalysisHandler`)
currently have no try/catch. When Claude returns a non-2xx response, the services
throw `InvalidOperationException`, which propagates as an unhandled exception and
produces a generic 500 with no structured error information.

This feature adds structured AI error envelopes. A static `AiErrorClassifier`
inspects the exception message to produce an `AiErrorEnvelope` record with a
`Code` (`api_error`, `output_validation`, `configuration_error`, `timeout`),
`IsRetryable` flag, and `Source` (which feature generated it). Each handler wraps
its AI service call in a try/catch, classifies the exception, records it in a
singleton `IAiErrorStore`, and returns a structured `Error.Failure` rather than
letting the exception propagate. `GET /api/admin/ai-errors` exposes recent envelopes.

No existing endpoints are modified in their request shape.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **5.3 — Error Envelopes** | `AiErrorClassifier.Classify(exception, source)` maps raw exceptions to structured `AiErrorEnvelope(Code, Source, Message, IsRetryable, OccurredAt)`; four handlers (`CritiqueRecipeHandler`, `ScaleRecipeHandler`, `ReviewRecipeDraftHandler`, `SubmitRecipeBatchAnalysisHandler`) wrap AI service calls in try/catch, record to `IAiErrorStore`, and return `Error.Failure($"AI.{envelope.Code}", ...)` — converting unstructured exceptions into typed, queryable error records |

---

## Architecture

```
POST /api/recipes/{id}/critique
        │  CritiqueRecipeHandler
        │  try {
        │    var critique = await _critiqueService.CritiqueAsync(dto, ct);
        │    return critique;
        │  } catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException) {
        │    var envelope = AiErrorClassifier.Classify(ex, "recipe-critique");
        │    _aiErrorStore.Record(envelope);
        │    return Error.Failure($"AI.{envelope.Code}", envelope.Message);
        │  }
        └── 500 Problem (before) → 500 structured AI error (after)

GET /api/admin/ai-errors?limit=N
        │  GetAiErrorsQuery
        │  └── IAiErrorStore.GetRecent(limit)
        └── IReadOnlyList<AiErrorEnvelope>
```

---

## AiErrorEnvelope + classification

```csharp
public sealed record AiErrorEnvelope(
    Guid Id,
    string Code,        // "api_error" | "output_validation" | "configuration_error" | "timeout"
    string Source,      // "recipe-critique" | "recipe-scaling" | "recipe-jury" | "recipe-batch"
    string Message,
    bool IsRetryable,
    DateTime OccurredAt);
```

**Classification rules** (evaluated in order):

| Exception message contains | Code | IsRetryable |
|---|---|---|
| `"api key"` or `"missing"` (case-insensitive) | `configuration_error` | false |
| `"timed out"` or `"timeout"` | `timeout` | true |
| `"deserializ"` or `"json"` or `"valid"` | `output_validation` | false |
| `"500"` or `"502"` or `"503"` or `"504"` | `api_error` | true |
| Anything else | `api_error` | true |

---

## IAiErrorStore

```csharp
public interface IAiErrorStore
{
    void Record(AiErrorEnvelope envelope);
    IReadOnlyList<AiErrorEnvelope> GetRecent(int limit);
}
```

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.Application/Common/AI/AiErrorEnvelope.cs` | `AiErrorEnvelope` record + `IAiErrorStore` interface |
| `Backend/src/Recipes.Application/Common/AI/AiErrorClassifier.cs` | Static classifier |
| `Backend/src/Recipes.Application/Admin/GetAiErrors/GetAiErrorsQuery.cs` | Query + handler |
| `Backend/src/Recipes.Infrastructure/AI/AiErrors/InMemoryAiErrorStore.cs` | Singleton store |
| `Backend/Docs/CCAF/C3-error-envelopes.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeCommand.cs` | Inject `IAiErrorStore`; wrap service call; catch + classify |
| `Backend/src/Recipes.Application/Recipes/ScaleRecipe/ScaleRecipeCommand.cs` | Same |
| `Backend/src/Recipes.Application/Recipes/ReviewRecipeDraft/ReviewRecipeDraftCommand.cs` | Same |
| `Backend/src/Recipes.Application/Recipes/BatchAnalyseRecipes/SubmitRecipeBatchAnalysisCommand.cs` | Same |
| `Backend/src/Recipes.Infrastructure/DependencyInjection.cs` | Register `IAiErrorStore` (singleton) |
| `Backend/src/Recipes.Api/Endpoints/AdminEndpoints.cs` | Add `GET /api/admin/ai-errors` |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. When `ClaudeOptions.ApiKey` is empty, `CritiqueRecipeHandler` returns `Error.Failure` with code `AI.configuration_error` rather than throwing.
3. When the Claude API returns a non-2xx (simulated), the handler returns `Error.Failure` with code `AI.api_error` and the error is recorded in `IAiErrorStore`.
4. `GET /api/admin/ai-errors` returns the recorded envelopes with the correct `code`, `source`, and `isRetryable` fields.
5. Existing `Recipes.Application.Tests` pass (51 tests).
