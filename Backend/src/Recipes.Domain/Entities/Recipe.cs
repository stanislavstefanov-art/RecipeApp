namespace Recipes.Domain.Entities;

using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

public sealed class Recipe : Entity
{
    private readonly List<RecipeIngredient> _ingredients = new();
    private readonly List<RecipeStep> _steps = new();
    private readonly List<RecipeVariation> _variations = new();

    public RecipeId Id { get; private set; } = RecipeId.New();
    public RecipeName Name { get; private set; }

    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();
    public IReadOnlyCollection<RecipeVariation> Variations => _variations.AsReadOnly();

    private Recipe() { }

    public Recipe(string name)
    {
        Name = new RecipeName(name);
        RaiseDomainEvent(new RecipeCreated(Id, Name));
    }

    public void Rename(string name)
    {
        var newName = new RecipeName(name);

        if (Name == newName)
        {
            return;
        }

        Name = newName;
        RaiseDomainEvent(new RecipeRenamed(Id, Name));
    }

    public void AddIngredient(string name, decimal quantity, string unit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ingredient name cannot be empty.", nameof(name));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit cannot be empty.", nameof(unit));
        }

        var ingredient = new RecipeIngredient(Id, name.Trim(), quantity, unit.Trim());
        _ingredients.Add(ingredient);

        RaiseDomainEvent(new RecipeIngredientAdded(
            Id,
            ingredient.Id,
            ingredient.Name,
            ingredient.Quantity,
            ingredient.Unit));
    }

    public void AddStep(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            throw new ArgumentException("Step instruction cannot be empty.", nameof(instruction));
        }

        var step = new RecipeStep(Id, _steps.Count + 1, instruction.Trim());
        _steps.Add(step);
        RaiseDomainEvent(new StepAdded(Id, step.Order, step.Instruction));
    }

    public RecipeVariation AddVariation(
        string name,
        string? notes = null,
        string? ingredientAdjustmentNotes = null)
    {
        var existing = _variations.SingleOrDefault(x =>
            string.Equals(x.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            throw new InvalidOperationException($"Variation '{name}' already exists for recipe '{Id}'.");
        }

        var variation = new RecipeVariation(Id, name, notes, ingredientAdjustmentNotes);
        _variations.Add(variation);

        return variation;
    }
}