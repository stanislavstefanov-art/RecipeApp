namespace Recipes.Application.Pantry;

public sealed record PantryItemDto(Guid Id, string IngredientName, string? Notes, DateTimeOffset CreatedAt);
