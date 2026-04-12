namespace Recipes.Application.MealPlans.ListMealPlans;

public sealed record MealPlanListItemDto(
    Guid Id,
    string Name,
    Guid HouseholdId,
    string HouseholdName,
    int EntryCount);