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

## Claude Code workflow guidance

- Use PLAN MODE for: new aggregates, adding AI features, any change touching 3+ files,
  architectural decisions, or anything involving the Claude API integration.
- Use DIRECT EXECUTION for: single-file bug fixes, adding a new endpoint to an existing
  slice, updating a validator, fixing a failing test.
- When context fills up during long sessions, use /compact before starting a new task.
- Use the scaffold-slice skill for scaffolding vertical slices — it runs in an
  isolated context via context:fork, keeping scaffolding output out of the main
  session. Invoke it by asking Claude Code to scaffold a feature by name.

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

## Recipe import
The recipe import flow is being prepared for Claude-backed structured extraction.

Current state:
- `IRecipeImportService` returns raw extraction results
- `RecipeImportOrchestrator` handles validation/retry/mapping
- `StubRecipeImportService` is the active implementation
- schema and prompt files live under `Docs/`

Guidance:
- keep extraction concerns in Infrastructure
- keep retry and validation orchestration outside the transport layer
- prefer explicit schema-driven extraction contracts