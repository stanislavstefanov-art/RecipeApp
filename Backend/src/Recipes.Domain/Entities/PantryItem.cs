namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class PantryItem
{
    public PantryItemId Id { get; private set; } = PantryItemId.New();
    public UserId UserId { get; private set; }
    public string IngredientName { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PantryItem() { }

    public PantryItem(UserId userId, string ingredientName, string? notes, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(ingredientName))
            throw new ArgumentException("Ingredient name cannot be empty.", nameof(ingredientName));

        UserId = userId;
        IngredientName = ingredientName.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = now;
    }
}
