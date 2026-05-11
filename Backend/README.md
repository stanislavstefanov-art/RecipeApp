# Recipes API

.NET 10 backend using Clean Architecture + CQRS (MediatR), EF Core, Minimal APIs, and FluentValidation.

## Prerequisites

- .NET 10 SDK
- SQL Server / LocalDB — **optional** (in-memory mode runs without it)

## Quick start

```bash
# First-time only — copy the dev config (file is gitignored)
cp src/Recipes.Api/appsettings.Development.json.example \
   src/Recipes.Api/appsettings.Development.json

dotnet run --project src/Recipes.Api
```

Defaults: in-memory database, stub AI services, demo data seeded on startup.

- API: `http://localhost:5106`
- Swagger UI: `http://localhost:5106/swagger`
- Health check: `GET /health`
- Demo login: `demo@local` / `demo1234`

## Running with real SQL Server

In `src/Recipes.Api/appsettings.Development.json`:

```json
{
  "Database": { "Provider": "SqlServer" },
  "ConnectionStrings": {
    "RecipesDb": "Server=(localdb)\\MSSQLLocalDB;Database=RecipesDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Migrations are applied automatically on startup.

## Enabling live Claude AI for one feature

Override via environment variable — no config file edit required:

```bash
RecipeImport__Provider=Claude \
Claude__ApiKey=sk-ant-... \
dotnet run --project src/Recipes.Api
```

## EF Core migrations (run from repo root)

```bash
dotnet ef migrations add <Name> \
  --project Backend/src/Recipes.Infrastructure \
  --startup-project Backend/src/Recipes.Api

dotnet ef database update --startup-project Backend/src/Recipes.Api

# Install the tool if missing:
dotnet tool install --global dotnet-ef
```

## Tests

```bash
# Unit tests — no external dependencies
dotnet test Recipes.sln --filter "Category!=Docker"

# Integration tests — requires Docker
dotnet test Recipes.sln --filter "Category=Docker"
```

## Architecture

Clean Architecture with CQRS. Dependency direction: `Api → Application ← Infrastructure`, `Domain` at the center.

| Project | Responsibility |
|---|---|
| `Recipes.Domain` | Aggregates, entities, strongly-typed IDs, value objects, domain events |
| `Recipes.Application` | Vertical slices: Command/Query + Handler + FluentValidation Validator |
| `Recipes.Infrastructure` | EF Core, `RecipesDbContext`, repository implementations |
| `Recipes.Api` | Minimal API endpoints, dispatches to MediatR |

## Architecture invariants (CI-enforced)

The `architecture-guard` CI workflow fails on any of these:

1. `IRecipesDbContext` referenced in `Recipes.Application`
2. Cross-aggregate access (`Ingredient`, `RecipeStep`) outside a `Recipe` traversal
3. New `*Command.cs` accepting user input without a matching `*Validator.cs`
4. New AI-using slice without a `Backend/Docs/CCAF/<id>-*.md` entry
