namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class ShoppingListItemSource
{
    public ShoppingListItemId ShoppingListItemId { get; private set; }
    public RecipeId RecipeId { get; private set; }
    public string RecipeName { get; private set; } = string.Empty;

    private ShoppingListItemSource() { }

    public decimal Portions { get; private set; }

    internal ShoppingListItemSource(ShoppingListItemId itemId, RecipeId recipeId, string recipeName, decimal portions = 1m)
    {
        ShoppingListItemId = itemId;
        RecipeId = recipeId;
        RecipeName = recipeName;
        Portions = portions;
    }
}
