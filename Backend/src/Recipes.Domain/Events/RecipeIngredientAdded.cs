using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record RecipeIngredientAdded(
    RecipeId RecipeId,
    RecipeIngredientId RecipeIngredientId,
    string Name,
    decimal Quantity,
    string Unit) : IDomainEvent;