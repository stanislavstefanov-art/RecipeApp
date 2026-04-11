using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record StepAdded(RecipeId RecipeId, int Order, string Instruction) : IDomainEvent;
