# Recipes Backend

Minimal .NET backend scaffold using Clean Architecture + CQRS (MediatR), EF Core, and Minimal APIs.

## Prerequisites

- .NET SDK (targets `net10.0`)
- Azure SQL (or SQL Server) connection string

## Configure connection string (local)

Set `ConnectionStrings:RecipesDb` for the API project.

Example (dev only) in `Backend/src/Recipes.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "RecipesDb": "Server=localhost;Database=Recipes;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Avoid committing secrets. Prefer environment variables / user-secrets for real credentials.

## Run API

From repo root:

```bash
dotnet run --project Backend/src/Recipes.Api
```

Swagger UI is enabled. Health check: `GET /health`.

## EF Core migrations

Add migration:

```bash
dotnet ef migrations add InitialCreate --project Backend/src/Recipes.Infrastructure --startup-project Backend/src/Recipes.Api
```

Update database:

```bash
dotnet ef database update --startup-project Backend/src/Recipes.Api
```

If `dotnet ef` isn't available, install the tool:

```bash
dotnet tool install --global dotnet-ef
```

## API endpoints (v1)

- `POST /api/recipes`
- `GET /api/recipes/{id}`
- `GET /api/recipes`
- `PUT /api/recipes/{id}`
- `DELETE /api/recipes/{id}`

