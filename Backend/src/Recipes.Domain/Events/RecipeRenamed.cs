using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record RecipeRenamed(RecipeId RecipeId, RecipeName NewName) : IDomainEvent;
