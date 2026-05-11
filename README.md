# RecipesApp

A full-stack cooking recipes application with AI-powered meal planning.

Two parallel frontends — React 19 and Angular — share the same .NET 10 API backend. Keeping both is intentional: a learning exercise to contrast the frameworks against the same domain.

## Tech stack

| Layer | Technology |
|---|---|
| API | .NET 10, Clean Architecture + CQRS (MediatR), EF Core, Minimal APIs |
| React | React 19, TypeScript, Vite, TanStack Query, Tailwind |
| Angular | Angular (zoneless, standalone, signals), TypeScript, Tailwind |
| Database | SQL Server / Azure SQL (in-memory for local dev) |
| AI | Anthropic Claude (claude-haiku-4-5) |
| Infra | Azure — App Service, Static Web Apps, Azure SQL, Key Vault |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+](https://nodejs.org/)
- SQL Server or LocalDB — **optional** (in-memory mode runs without it)
- Anthropic API key — **optional** (stub mode runs without it)

## Quick start — no infrastructure required

The default development config uses an in-memory database and stub AI services, so the full stack runs on a fresh clone with no external dependencies.

### 1. Configure the backend (first-time only)

```bash
cp Backend/src/Recipes.Api/appsettings.Development.json.example \
   Backend/src/Recipes.Api/appsettings.Development.json
```

### 2. Start the backend

```bash
dotnet run --project Backend/src/Recipes.Api
```

API: `http://localhost:5106` · Swagger UI: `http://localhost:5106/swagger`

### 3. Start the React frontend

```bash
cd Frontend
npm install
npm run dev
```

React app: `http://localhost:5173`

### 4. Start the Angular frontend

```bash
cd FrontendAngular
npm install --legacy-peer-deps
npm run start
```

Angular app: `http://localhost:4200`

### Demo credentials

The seeder creates these on every backend start (in-memory mode resets on shutdown):

| Field | Value |
|---|---|
| Email | `demo@local` |
| Password | `demo1234` |

## Running with a real SQL Server

In `Backend/src/Recipes.Api/appsettings.Development.json`:

```json
{
  "Database": { "Provider": "SqlServer" },
  "ConnectionStrings": {
    "RecipesDb": "Server=(localdb)\\MSSQLLocalDB;Database=RecipesDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Migrations are applied automatically on startup.

## Enabling live Claude AI for a feature

Override any AI service via environment variable — no config file edit needed:

```bash
RecipeImport__Provider=Claude \
Claude__ApiKey=sk-ant-... \
dotnet run --project Backend/src/Recipes.Api
```

Available provider switches: `RecipeImport`, `MealPlanSuggestion`, `IngredientSubstitution`, `RecipeCritique`, `RecipeScaling`, `RecipeBatchAnalysis`, `RecipeDraftReview`, `ExpenseInsight`.

## Tests

```bash
# Backend — unit tests (no external dependencies)
dotnet test Backend/Recipes.sln --filter "Category!=Docker"

# Backend — integration tests (requires Docker)
dotnet test Backend/Recipes.sln --filter "Category=Docker"

# React — lint + build
cd Frontend && npm run lint && npm run build

# Angular — lint + build + unit tests
cd FrontendAngular && npm run lint && npm run build && npm test

# Angular — e2e (Playwright, no running backend needed)
cd FrontendAngular && npx playwright test
```

## Project structure

```
/Backend          — .NET 10 Clean Architecture API
/Frontend         — React 19 + TypeScript + Vite + TanStack Query
/FrontendAngular  — Angular (zoneless, standalone, signals)
/infra            — Bicep IaC templates for Azure
/Docs             — Architecture Decision Records, specs, plans
```

## Deploying to Azure

See [Docs/deployment/azure.md](Docs/deployment/azure.md).
