using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record RecipeCreated(RecipeId RecipeId, RecipeName Name) : IDomainEvent;
