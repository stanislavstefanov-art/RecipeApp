using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record MealPlanCreated(
    MealPlanId MealPlanId,
    string Name) : IDomainEvent;