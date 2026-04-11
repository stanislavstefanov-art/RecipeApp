---
context: fork
allowed-tools: Read, Write, Edit, Glob, Grep
argument-hint: "feature name e.g. GetRecipeById, CreateRecipe"
---

# Scaffold vertical slice

Scaffold a complete CQRS vertical slice for: $ARGUMENTS

## Steps

1. Use Glob to find an existing slice in Backend/src/Recipes.Application/Recipes/
   as a reference — read one handler and one command to understand the exact
   patterns in use before generating anything.

2. Create the following files in
   Backend/src/Recipes.Application/Recipes/$ARGUMENTS/:

   {Feature}Request.cs
   — inbound DTO, only if the feature accepts user input
   — record type, properties match what the API endpoint receives

   {Feature}Command.cs or {Feature}Query.cs
   — IRequest<ErrorOr<T>> via MediatR
   — Command for state-changing operations, Query for reads

   {Feature}Handler.cs
   — IRequestHandler<TCommand, ErrorOr<T>>
   — depends only on IRecipeRepository, never IRecipesDbContext directly
   — returns ErrorOr<T> — use Errors classes for domain errors

   {Feature}Validator.cs
   — AbstractValidator<{Feature}Command> or Query
   — only create if the feature accepts user input
   — validate all required fields, string lengths, guid formats

3. After creating files:
   - Wire the new endpoint in RecipesEndpoints.cs
   - Output a summary of files created and any domain methods on the Recipe
     aggregate that the handler will need

## Constraints

- Read existing slices first — match the exact code style already in use
- Never use IRecipesDbContext in handlers
- Never hardcode GUIDs or strings
- If $ARGUMENTS is ambiguous (e.g. "recipe tags"), ask one clarifying question
  before generating any files