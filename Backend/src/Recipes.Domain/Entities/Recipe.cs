namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class Recipe : Entity
{
    private readonly List<Ingredient> _ingredients = new();
    private readonly List<RecipeStep> _steps = new();

    public RecipeId Id { get; private set; } = RecipeId.New();
    public RecipeName Name { get; private set; }

    public IReadOnlyCollection<Ingredient> Ingredients => _ingredients.AsReadOnly();
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();

    private Recipe() { }

    public Recipe(string name)
    {
        Rename(name);
    }

    public void Rename(string name)
    {
        Name = new RecipeName(name);
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

        _ingredients.Add(new Ingredient(Id, name.Trim(), quantity, unit.Trim()));
    }

    public void AddStep(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            throw new ArgumentException("Step instruction cannot be empty.", nameof(instruction));
        }

        _steps.Add(new RecipeStep(Id, _steps.Count + 1, instruction.Trim()));
    }
}

