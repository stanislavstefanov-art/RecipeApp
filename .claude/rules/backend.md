---
paths: ["Backend/**/*"]
---

## Backend conventions

- All commands and queries use MediatR (CQRS). Never call handlers directly.
- Commands: Recipes/Application/{Feature}/{Feature}Command.cs
- Queries:  Recipes/Application/{Feature}/{Feature}Query.cs
- Handlers always return Result<T> — use ErrorOr package.
- FluentValidation validators for every command that accepts user input.
- Domain entities: private setters only, factory methods for creation,
  domain events raised on state changes.
- Repositories: interfaces in Domain, implementations in Infrastructure.
- Never expose EF Core DbContext outside Infrastructure layer.
- API controllers are thin — dispatch to MediatR, map result to HTTP response only.