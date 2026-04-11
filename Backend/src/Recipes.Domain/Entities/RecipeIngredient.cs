namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class RecipeIngredient
{
    public RecipeIngredientId Id { get; private set; } = RecipeIngredientId.New();
    public RecipeId RecipeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;

    private RecipeIngredient() { }

    internal RecipeIngredient(RecipeId recipeId, string name, decimal quantity, string unit)
    {
        RecipeId = recipeId;
        Name = name;
        Quantity = quantity;
        Unit = unit;
    }
}