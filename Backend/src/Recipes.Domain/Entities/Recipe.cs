namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

public sealed class Recipe : Entity
{
    private readonly List<RecipeIngredient> _ingredients = new();
    private readonly List<RecipeStep> _steps = new();
    private readonly List<RecipeVariation> _variations = new();
    private readonly List<RecipeRating> _ratings = new();

    public RecipeId Id { get; private set; } = RecipeId.New();
    public RecipeName Name { get; private set; }
    public HouseholdId? HouseholdId { get; private set; }
    public RecipeType RecipeType { get; private set; } = RecipeType.MainDish;
    public bool IsImported { get; private set; }

    public string? ImageUrl { get; private set; }

    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();
    public IReadOnlyCollection<RecipeVariation> Variations => _variations.AsReadOnly();
    public IReadOnlyCollection<RecipeRating> Ratings => _ratings.AsReadOnly();

    public double? AverageStars =>
        _ratings.Count > 0 ? Math.Round(_ratings.Average(r => r.Stars), 1) : null;
    public int RatingCount => _ratings.Count;

    private Recipe() { }

    public Recipe(string name, HouseholdId? householdId = null)
    {
        Name = new RecipeName(name);
        HouseholdId = householdId;
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

    public bool RemoveIngredient(RecipeIngredientId ingredientId)
    {
        var ingredient = _ingredients.SingleOrDefault(i => i.Id == ingredientId);
        if (ingredient is null) return false;

        _ingredients.Remove(ingredient);
        return true;
    }

    public bool RemoveStep(RecipeStepId stepId)
    {
        var step = _steps.SingleOrDefault(s => s.Id == stepId);
        if (step is null) return false;

        _steps.Remove(step);
        var remaining = _steps.OrderBy(s => s.Order).ToList();
        for (var i = 0; i < remaining.Count; i++)
            remaining[i].SetOrder(i + 1);

        return true;
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

    public RecipeRating Rate(UserId userId, int stars, string? comment, DateTimeOffset now)
    {
        if (stars < 1 || stars > 5)
            throw new ArgumentOutOfRangeException(nameof(stars), "Stars must be between 1 and 5.");

        var existing = _ratings.SingleOrDefault(r => r.UserId == userId);
        if (existing is not null)
        {
            existing.Update(stars, comment, now);
            return existing;
        }

        var rating = new RecipeRating(Id, userId, stars, comment, now);
        _ratings.Add(rating);
        return rating;
    }

    public bool RemoveRating(UserId userId)
    {
        var existing = _ratings.SingleOrDefault(r => r.UserId == userId);
        if (existing is null) return false;
        _ratings.Remove(existing);
        return true;
    }

    public void SetImageUrl(string? url) => ImageUrl = url;

    public void SetRecipeType(RecipeType type) => RecipeType = type;

    public void MarkAsImported() => IsImported = true;
}