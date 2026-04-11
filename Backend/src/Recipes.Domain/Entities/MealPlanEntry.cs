namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;

public sealed class MealPlanEntry
{
    public MealPlanEntryId Id { get; private set; } = MealPlanEntryId.New();
    public MealPlanId MealPlanId { get; private set; }
    public RecipeId RecipeId { get; private set; }
    public DateOnly PlannedDate { get; private set; }
    public MealType MealType { get; private set; }

    private MealPlanEntry() { }

    internal MealPlanEntry(
        MealPlanId mealPlanId,
        RecipeId recipeId,
        DateOnly plannedDate,
        MealType mealType)
    {
        MealPlanId = mealPlanId;
        RecipeId = recipeId;
        PlannedDate = plannedDate;
        MealType = mealType;
    }
}