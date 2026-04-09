# CLAUDE.md

@import .claude/rules/backend.md
@import .claude/rules/frontend.md
@import .claude/rules/testing.md

## Project overview

RecipeApp — a cooking recipes application with AI-powered meal planning.
Full-stack: React 19 frontend + .NET 10 API backend, deployed to Azure.

## Repository structure

- /Backend    — .NET 10 Clean Architecture API (Domain, Application, Infrastructure, Api)
- /frontend   — React 19 + TypeScript + Vite + TanStack Query + Tailwind (not yet created)
- /infra      — Bicep IaC templates for Azure deployment (not yet created)
- /docs       — Architecture Decision Records

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

## Adding a new feature

Use the /new-feature slash command or follow the pattern manually:

  Backend/src/Recipes.Application/Recipes/{FeatureName}/
    {FeatureName}Request.cs      # optional inbound DTO
    {FeatureName}Command.cs      # or Query — IRequest<T>
    {FeatureName}Handler.cs      # IRequestHandler<TRequest, TResponse>
    {FeatureName}Validator.cs    # FluentValidation when input validation needed

Then wire the endpoint in RecipesEndpoints.cs.

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
- Use /new-feature skill for scaffolding vertical slices — it runs with context:fork to
  keep scaffolding output isolated from the main session.

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
- Ingredient substitution suggestions (claude-haiku-3-5, synchronous)
- Weekly meal plan generation (multi-agent, claude-haiku-3-5 for subagents)
- Recipe extraction from unstructured text (tool_use + JSON schema)

Use claude-haiku-3-5 for all runtime app calls. Use claude-sonnet-4-5 only for
Claude Code sessions. Never call claude-sonnet-4-5 from application code — cost.