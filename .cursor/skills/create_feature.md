# Skill: Create CQRS Vertical Slice

Create a new feature following the vertical slice architecture.

Requirements:

- Follow Clean Architecture rules
- Place feature in `Recipes.Application/Recipes/{FeatureName}`
- Use MediatR for request handling
- Include:
  - Request DTO
  - Command or Query
  - Handler
  - Validator (FluentValidation)

Structure:

Recipes.Application
└─ Recipes
   └─ {FeatureName}
      ├─ {FeatureName}Request.cs
      ├─ {FeatureName}Command.cs or {FeatureName}Query.cs
      ├─ {FeatureName}Handler.cs
      └─ {FeatureName}Validator.cs

Guidelines:

- Commands modify state
- Queries return DTOs
- Handlers should depend only on abstractions
- Use IRecipesDbContext for persistence