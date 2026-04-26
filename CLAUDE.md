# CLAUDE.md

@import .claude/rules/backend.md
@import .claude/rules/frontend-react.md
@import .claude/rules/frontend-angular.md
@import .claude/rules/testing.md

## Project overview

RecipeApp — a cooking recipes application with AI-powered meal planning.
Full-stack: .NET 10 API backend plus **two parallel frontends** (React 19 and
Angular) sharing the same API. Keeping both is intentional — a learning
exercise to contrast the frameworks against the same domain. Do not share
source code between them.

Deployed to Azure.

## Repository structure

- /Backend         — .NET 10 Clean Architecture API (Domain, Application, Infrastructure, Api)
- /Frontend        — React 19 + TypeScript + Vite + TanStack Query + Tailwind
- /FrontendAngular — Angular (zoneless, standalone, signals) + Tailwind — scaffold pending
- /infra           — Bicep IaC templates for Azure deployment (not yet created)
- /Docs            — Architecture Decision Records and reference material

The `.claude/rules/frontend-react.md` and `.claude/rules/frontend-angular.md`
rule files are `paths:`-scoped so Claude only applies the matching framework's
rules when working inside each folder.

## Commands

### Backend
dotnet run --project Backend/src/Recipes.Api
dotnet build Backend/Recipes.sln
dotnet test Backend/Recipes.sln

### EF Core migrations (from repo root)
dotnet ef migrations add <Name> \
  --project Backend/src/Recipes.Infrastructure \
  --startup-project Backend/src/Recipes.Api
dotnet ef database update --startup-project Backend/src/Recipes.Api

### Frontend (React) — from /Frontend
npm run dev        # http://localhost:5173
npm run build
npm run lint

### Frontend (Angular) — from /FrontendAngular (once scaffolded)
npm run start      # http://localhost:4200
npm run build
npm run lint

Backend CORS in development must allow both `http://localhost:5173` and
`http://localhost:4200`.

## Architecture

Clean Architecture with CQRS targeting .NET 10.
Dependency direction: Api → Application ← Infrastructure, Domain at center.

- Recipes.Domain        — Aggregates, entities, strongly-typed IDs, value objects,
                          domain events. No anemic models — logic belongs on aggregates.
- Recipes.Application   — Vertical slices under Recipes/{FeatureName}/. Each slice:
                          Command/Query (MediatR IRequest<T>), Handler, optional DTO,
                          optional FluentValidation Validator.
- Recipes.Infrastructure — RecipesDbContext implements IRecipesDbContext. EF Core Fluent
                           configs in Persistence/Configurations/.
- Recipes.Api           — Minimal API. RecipesEndpoints.cs delegates to MediatR.

## Key conventions

- Strongly-typed IDs: readonly record struct with New() and From(Guid) factory methods.
  From() guards against empty GUIDs.
- EF Core value conversions in Configurations/ — never rely on EF conventions for
  domain types.
- Recipe is the only aggregate root. Ingredient and RecipeStep accessed through it only.
- Cascade deletes configured: deleting a Recipe removes its Ingredients and Steps.
- Connection string in Backend/src/Recipes.Api/appsettings.Development.json under
  ConnectionStrings:RecipesDb (SQL Server / Azure SQL).

## Using the RecipeApp agent

The `recipe-assistant` sub-agent (`.claude/agents/recipe-assistant.md`) has direct
access to the live RecipesApp API through the `recipes-app` MCP server. Use it for
data queries — it has no code-editing tools.

**Prerequisite:** the API must be running:
```bash
dotnet run --project Backend/src/Recipes.Api
```

Invoke with the `@recipe-assistant` prefix or just describe a data query:

```
@recipe-assistant What recipes contain chicken?
@recipe-assistant Show me this week's meal plan
@recipe-assistant What's still pending on the shopping list?
@recipe-assistant What was last month's total spend?
```

Claude Code routes the request to the agent automatically based on context. The MCP
server subprocess is started on first use — allow a few seconds on the first query.

## Claude Code workflow guidance

- Use PLAN MODE for: new aggregates, adding AI features, any change touching 3+ files,
  architectural decisions, or anything involving the Claude API integration.
- Use DIRECT EXECUTION for: single-file bug fixes, adding a new endpoint to an existing
  slice, updating a validator, fixing a failing test.
- When context fills up during long sessions, use /compact before starting a new task.
- Use the scaffold-slice skill for scaffolding vertical slices — it runs in an
  isolated context via context:fork, keeping scaffolding output out of the main
  session. Invoke it by asking Claude Code to scaffold a feature by name.
- Use `/refine <path>` to run a three-pass critique → revise → re-critique loop on a
  single source file under 500 lines. Runs in a forked context.
- Use `/spec-from-issue <number>` to draft a `Docs/specs/<n>-*.md` from a GitHub issue.
  The command writes the spec only — drafting the plan is a separate step so you can
  review the spec first.
- Use `/architecture-check` to run the four-rule architecture invariant check locally
  (same rules enforced by the `architecture-guard` CI workflow).

## Architecture invariants (CI-enforced)

The `architecture-guard` workflow (`.github/workflows/architecture-guard.yml`)
fails the PR check on any of these violations:

1. `IRecipesDbContext` referenced in `Recipes.Application/**`.
2. Cross-aggregate access (`Ingredient`, `RecipeStep` outside a `Recipe` traversal)
   in `Recipes.Application/**`.
3. New `*Command.cs` accepting user input without a matching `*Validator.cs`.
4. New AI-using slice without a `Backend/Docs/CCAF/<id>-*.md` entry.

The full rule definitions live in `.claude/commands/architecture-check.md`,
invokable locally as `/architecture-check`.

## Automated PR review

Every non-draft pull request gets an automated advisory review from Claude
(claude-haiku-4-5) running the `/review` slash command. Findings are posted as
review comments. The review is advisory — it never blocks merge. Blocking
enforcement is handled by the `architecture-guard` workflow.

## Mention bot

Comment `@claude <question>` on any issue or pull-request thread to ask Claude
(claude-haiku-4-5) about the codebase. The bot is read-only — it can inspect
files and git history but cannot push commits or open PRs. Disable temporarily
by setting the repo variable `CLAUDE_BOT_ENABLED=false`.

## Frontend implementation guidance

Framework-specific rules live in:
- `.claude/rules/frontend-react.md` (scoped to `/Frontend`)
- `.claude/rules/frontend-angular.md` (scoped to `/FrontendAngular`)

Cross-framework guidance (applies to both):
- Build one vertical slice at a time.
- Organize by feature folders, not by file type.
- Keep API calls in a dedicated `api/` folder — never fetch from components.
- Consume backend read models directly rather than reconstructing names from ids.
- Every page/major component must explicitly handle loading, error, and empty states.
- Do not share source code between the React and Angular apps — reimplement
  each slice idiomatically per framework.

## Azure deployment targets

- API        → Azure App Service (F1 free tier, Linux, .NET 10)
- Frontend   → Azure Static Web Apps (free tier)
- Database   → Azure SQL (free tier / serverless)
- Secrets    → Azure Key Vault (ANTHROPIC_API_KEY stored here — never hardcode)
- Monitoring → Application Insights
- IaC        → Bicep templates in /infra

Never hardcode connection strings or API keys. Always use environment variables or
Key Vault references. When generating infrastructure code, default to free-tier SKUs.

## AI features (Claude API integration)

The app integrates Claude for:
- Ingredient substitution suggestions (claude-haiku-4-5, synchronous)
- Weekly meal plan generation (multi-agent, claude-haiku-4-5 for subagents)
- Recipe extraction from unstructured text (tool_use + JSON schema)

Use claude-haiku-4-5 for all runtime app calls. Use claude-sonnet-4-5 only for
Claude Code sessions. Never call claude-sonnet-4-5 from application code — cost.

## Local mock mode (no Anthropic API key required)

Default `dotnet run` (Development environment) routes every AI service to a
synthetic stub and seeds demo data on first launch — no Anthropic API key
needed. Useful for end-to-end demos without spending tokens.

First-time setup on a fresh clone:

```bash
cp Backend/src/Recipes.Api/appsettings.Development.json.example \
   Backend/src/Recipes.Api/appsettings.Development.json
dotnet run --project Backend/src/Recipes.Api
```

The example file flips every `*:Provider` switch to `Stub`, leaves `Claude:ApiKey`
blank, and sets `Seed:Enabled=true`. The seeder runs once when the `Recipes`
table is empty and inserts ~10 recipes, 2 households with 5 members, a weekly
meal plan, a half-purchased shopping list, and 10 expenses across the current
and previous month.

Flip a single feature back to live Claude during dev (env var, no file edit):

```bash
RecipeImport__Provider=Claude   Claude__ApiKey=sk-ant-... \
dotnet run --project Backend/src/Recipes.Api
```

Reset demo data: `dotnet ef database drop --startup-project Backend/src/Recipes.Api -f`
then re-run.

What stubs do **not** exercise:
- Anthropic SDK validation, real-model latency, prompt-token accounting
- Batch-API polling lifecycle (the stub returns `ended` immediately)
- Provenance and confidence-calibration paths that depend on real model output

If you need to test those paths specifically, set `Claude__ApiKey` and flip the
relevant `*:Provider` to `Claude`.

## Recipe import
The recipe import flow uses Claude-backed structured extraction by default.

Current state:
- `IRecipeImportService` returns raw extraction results
- `RecipeImportOrchestrator` handles validation/retry/mapping
- `ClaudeRecipeImportService` is the active implementation (via `RecipeImport:Provider=Claude` in appsettings)
- `StubRecipeImportService` is selected only when `RecipeImport:Provider=Stub` (used in tests)
- schema and prompt files live under `Backend/Docs/`

Guidance:
- keep extraction concerns in Infrastructure
- keep retry and validation orchestration outside the transport layer
- prefer explicit schema-driven extraction contracts