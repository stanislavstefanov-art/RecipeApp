namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

public sealed class MealPlan : Entity
{
    private readonly List<MealPlanEntry> _entries = new();

    public MealPlanId Id { get; private set; } = MealPlanId.New();
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<MealPlanEntry> Entries => _entries.AsReadOnly();

    private MealPlan() { }

    public MealPlan(string name)
    {
        Rename(name);
        RaiseDomainEvent(new MealPlanCreated(Id, Name));
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Meal plan name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public void AddRecipe(Recipe recipe, DateOnly plannedDate, MealType mealType)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        var duplicate = _entries.Any(x =>
            x.RecipeId == recipe.Id &&
            x.PlannedDate == plannedDate &&
            x.MealType == mealType);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Recipe '{recipe.Id}' is already planned for {plannedDate:yyyy-MM-dd} as {mealType}.");
        }

        var entry = new MealPlanEntry(Id, recipe.Id, plannedDate, mealType);
        _entries.Add(entry);

        RaiseDomainEvent(new MealPlanRecipeAdded(
            Id,
            entry.Id,
            entry.RecipeId,
            entry.PlannedDate,
            entry.MealType));
    }

    public void RemoveEntry(MealPlanEntryId entryId)
    {
        var entry = _entries.SingleOrDefault(x => x.Id == entryId)
                   ?? throw new InvalidOperationException($"Meal plan entry '{entryId}' was not found.");

        _entries.Remove(entry);

        RaiseDomainEvent(new MealPlanEntryRemoved(
            Id,
            entry.Id,
            entry.RecipeId,
            entry.PlannedDate,
            entry.MealType));
    }
}