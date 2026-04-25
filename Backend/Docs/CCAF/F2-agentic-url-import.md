# F2 — Agentic URL Import: CCAF Coverage

## What this implements

A multi-turn Claude agentic loop that accepts a recipe webpage URL, fetches and
cleans the HTML, extracts structured recipe fields, normalizes every ingredient in
parallel, and returns an `ImportedRecipeDto` — all driven by `tool_use` rather than
JSON-in-prompt. Lives alongside the existing text import; nothing from the original
implementation is modified.

Entry point: `POST /api/recipes/import/url`

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| Tool `input_schema` design | `RecipeImportTools.cs` — four `JsonDocument` schemas as static fields |
| Tool description writing (guides model behaviour) | Each `ClaudeToolDefinition.Description` — explains what the tool returns, when to call it, and what errors look like |
| Multi-turn agentic loop (`stop_reason: tool_use`) | `RecipeImportAgent.RunAsync` — iterates up to `MaxIterations`, appending assistant + tool-result messages each turn |
| Parallel tool calls | `Task.WhenAll(toolUseBlocks.Select(DispatchToolAsync))` — all `tool_use` blocks from one response execute concurrently |
| Tool error handling (`is_error: true`) | `ExecuteFetchUrlAsync` returns `is_error: true` on non-2xx or exception; system prompt instructs Claude to retry once then fall back to `save_recipe` with `needsReview: true` |
| Structured output via terminal tool | `save_recipe` input IS the `ImportedRecipeDto` — no JSON-in-text parsing needed |
| `tool_choice: auto` | `ClaudeAgentRequest.ToolChoice = new ClaudeToolChoiceAuto()` — Claude decides the call sequence |

## Key design decisions

### Why `tool_choice: auto` instead of forced?

`auto` lets Claude orchestrate the multi-step sequence itself, which is the real
agentic pattern. The model must reason about which tool to call next based on prior
results. Forced choice (`{type:"tool",name:"..."}`) is a single-turn structured-output
technique (covered by F4's prompts primitive). Both patterns are covered across the
feature set; keeping them separate makes the contrast explicit.

### Why is `normalize_ingredient` a separate tool?

It demonstrates **parallel tool calls**: Claude emits one `tool_use` block per
ingredient in a single response, and the backend executes all of them concurrently via
`Task.WhenAll`. If normalization were done silently inside `save_recipe`, the parallel
execution pattern would not be visible in the conversation trace or tests.

### Why does `extract_recipe_fields` exist before `save_recipe`?

It adds an intermediate turn where the backend can validate the draft (non-empty
ingredients and steps) and tell Claude how many ingredients need normalization. This
makes the multi-turn structure observable in logs and gives the model explicit
confirmation before it proceeds. It also allows tests to assert on a specific
conversation shape.

### Why is the content block a single "fat" record?

The Claude API uses polymorphic content arrays (text, tool_use, tool_result blocks).
Rather than a complex `[JsonPolymorphic]` hierarchy, a single record with nullable
fields and `JsonIgnore(WhenWritingNull)` keeps serialization straightforward and
avoids custom converters. The `Type` field discriminates at read time.

### Comparison to the existing JSON-in-prompt import

| Aspect | Existing (`ClaudeRecipeImportClient`) | F2 (`RecipeImportAgent`) |
|---|---|---|
| Output mechanism | Parse JSON from text block | Read `save_recipe` tool input |
| Error recovery | Manual retry with concatenated prompt | Claude handles retry via `is_error: true` feedback |
| Ingredient normalization | Not done | Parallel per-ingredient tool calls |
| Conversation turns | 1–2 | 4–5 (fetch → extract → normalize → save) |
| Observability | Single log entry | One log entry per tool call + iteration |
| Schema location | `Docs/recipe-import-schema.json` | Inlined in `RecipeImportTools.cs` |
