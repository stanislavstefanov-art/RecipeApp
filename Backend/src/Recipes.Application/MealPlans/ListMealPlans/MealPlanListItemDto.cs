namespace Recipes.Application.MealPlans.ListMealPlans;

public sealed record MealPlanListItemDto(
    Guid Id,
    string Name,
    int EntryCount);