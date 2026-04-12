namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class RecipeVariationIngredientOverride
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public RecipeVariationId RecipeVariationId { get; private set; }
    public string IngredientName { get; private set; } = string.Empty;
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
    public bool IsRemoved { get; private set; }

    private RecipeVariationIngredientOverride() { }

    internal RecipeVariationIngredientOverride(
        RecipeVariationId recipeVariationId,
        string ingredientName,
        decimal? quantity,
        string? unit,
        bool isRemoved)
    {
        if (string.IsNullOrWhiteSpace(ingredientName))
        {
            throw new ArgumentException("Ingredient name cannot be empty.", nameof(ingredientName));
        }

        if (!isRemoved && quantity.HasValue && quantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (!isRemoved && quantity.HasValue && string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit is required when quantity is provided.", nameof(unit));
        }

        RecipeVariationId = recipeVariationId;
        IngredientName = ingredientName.Trim();
        Quantity = quantity;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        IsRemoved = isRemoved;
    }

    internal void Update(decimal? quantity, string? unit, bool isRemoved)
    {
        if (!isRemoved && quantity.HasValue && quantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (!isRemoved && quantity.HasValue && string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit is required when quantity is provided.", nameof(unit));
        }

        Quantity = quantity;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        IsRemoved = isRemoved;
    }
}