Scaffold a complete vertical slice for the feature: $ARGUMENTS

Create the following files in Backend/src/Recipes.Application/Recipes/$ARGUMENTS/:
1. {Feature}Request.cs — inbound DTO (if the feature accepts input)
2. {Feature}Command.cs or {Feature}Query.cs — IRequest<Result<T>>
3. {Feature}Handler.cs — IRequestHandler, depends only on IRecipesDbContext
4. {Feature}Validator.cs — FluentValidation rules for the command/query

Then show me where to add the endpoint in RecipesEndpoints.cs.

Follow all conventions in CLAUDE.md. Use ErrorOr for the result type.