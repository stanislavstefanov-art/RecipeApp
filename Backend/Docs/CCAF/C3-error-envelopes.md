# C3 — AI Error Envelopes

## What this implements

Structured classification of AI-layer exceptions into typed error envelopes. Every handler that calls an AI service wraps the call in a try/catch that converts raw exceptions into `AiErrorEnvelope` records bearing a canonical `Code`, a `Source` tag, and an `IsRetryable` flag. Envelopes are stored in-process via `IAiErrorStore` and surfaced through `GET /api/admin/ai-errors`.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| 5.3 Structured error envelopes | `AiErrorEnvelope.cs`, `AiErrorClassifier.cs` |
| 5.3 Error classification (code taxonomy) | `AiErrorClassifier.ClassifyCode` — maps message keywords to `configuration_error`, `timeout`, `output_validation`, `api_error` |
| 5.3 IsRetryable semantics | `AiErrorEnvelope.IsRetryable` — `true` for `api_error` and `timeout`; `false` for `configuration_error` and `output_validation` |
| 5.3 Source tagging | `source` parameter passed per handler: `recipe-critique`, `recipe-scaling`, `recipe-jury`, `recipe-batch` |
| 5.3 Error observability endpoint | `GET /api/admin/ai-errors` → `GetAiErrorsQuery` → `IAiErrorStore.GetRecent` |

## Key decisions

- **Exception filter `when (ex is InvalidOperationException or HttpRequestException)`** — the two exception types the existing Claude services can throw. Anything else propagates normally, since it signals a programming error rather than an AI failure.
- **Keyword-based classifier** — parses the exception message rather than type-checking exception subclasses, because Claude HTTP clients wrap errors as `InvalidOperationException` with descriptive messages; the message carries more signal than the type.
- **`IsRetryable` on the envelope** — keeps retry decision out of the caller; a future retry middleware can check this flag without re-classifying.
- **`IAiErrorStore` as singleton** — matches the same pattern as `IProvenanceStore` and `IConfidenceCalibrationStore`; error history must survive individual request scopes.
- **`GetRecent(limit)` returns newest-first** — achieved by `TakeLast(limit).Reverse()` on the `ConcurrentQueue`, matching the provenance endpoint convention.
