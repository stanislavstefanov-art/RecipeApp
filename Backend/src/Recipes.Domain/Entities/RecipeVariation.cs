namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class RecipeVariation
{
    private readonly List<RecipeVariationIngredientOverride> _ingredientOverrides = [];

    public RecipeVariationId Id { get; private set; } = RecipeVariationId.New();
    public RecipeId RecipeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public string? IngredientAdjustmentNotes { get; private set; }
    public IReadOnlyCollection<RecipeVariationIngredientOverride> IngredientOverrides => _ingredientOverrides.AsReadOnly();

    private RecipeVariation() { }

    internal RecipeVariation(
        RecipeId recipeId,
        string name,
        string? notes,
        string? ingredientAdjustmentNotes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variation name cannot be empty.", nameof(name));
        }

        RecipeId = recipeId;
        Name = name.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        IngredientAdjustmentNotes = string.IsNullOrWhiteSpace(ingredientAdjustmentNotes)
            ? null
            : ingredientAdjustmentNotes.Trim();
    }

    internal void Update(
        string name,
        string? notes,
        string? ingredientAdjustmentNotes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variation name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        IngredientAdjustmentNotes = string.IsNullOrWhiteSpace(ingredientAdjustmentNotes)
            ? null
            : ingredientAdjustmentNotes.Trim();
    }

    public void RemoveIngredient(string ingredientName)
    {
        var existing = _ingredientOverrides.SingleOrDefault(x =>
            string.Equals(x.IngredientName, ingredientName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Update(null, null, true);
            return;
        }

        _ingredientOverrides.Add(new RecipeVariationIngredientOverride(
            Id,
            ingredientName,
            null,
            null,
            true));
    }

    public void OverrideIngredient(string ingredientName, decimal quantity, string unit)
    {
        var existing = _ingredientOverrides.SingleOrDefault(x =>
            string.Equals(x.IngredientName, ingredientName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Update(quantity, unit, false);
            return;
        }

        _ingredientOverrides.Add(new RecipeVariationIngredientOverride(
            Id,
            ingredientName,
            quantity,
            unit,
            false));
    }
}