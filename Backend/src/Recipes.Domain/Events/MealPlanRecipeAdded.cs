using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record MealPlanRecipeAdded(
    MealPlanId MealPlanId,
    MealPlanEntryId MealPlanEntryId,
    RecipeId RecipeId,
    DateOnly PlannedDate,
    MealType MealType) : IDomainEvent;