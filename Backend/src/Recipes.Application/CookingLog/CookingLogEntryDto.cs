namespace Recipes.Application.CookingLog;

public sealed record CookingLogEntryDto(
    Guid Id,
    Guid RecipeId,
    string RecipeName,
    DateOnly CookedOn,
    int Servings,
    string? Notes,
    DateTimeOffset CreatedAt);
