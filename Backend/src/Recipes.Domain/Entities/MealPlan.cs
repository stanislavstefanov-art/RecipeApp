namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

public sealed class MealPlan : Entity
{
    private readonly List<MealPlanEntry> _entries = [];

    public MealPlanId Id { get; private set; } = MealPlanId.New();
    public HouseholdId HouseholdId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<MealPlanEntry> Entries => _entries.AsReadOnly();

    private MealPlan() { }

    public MealPlan(string name, HouseholdId householdId)
    {
        HouseholdId = householdId;
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

    public void AddRecipe(
        Recipe baseRecipe,
        DateOnly plannedDate,
        MealType mealType,
        MealScope scope,
        IReadOnlyList<(PersonId PersonId, RecipeId AssignedRecipeId, RecipeVariationId? RecipeVariationId, decimal PortionMultiplier, string? Notes)> assignments,
        RecipeId? saladRecipeId = null)
    {
        ArgumentNullException.ThrowIfNull(baseRecipe);

        if (assignments.Count == 0)
        {
            throw new InvalidOperationException("At least one person assignment is required.");
        }

        var duplicate = _entries.Any(x =>
            x.PlannedDate == plannedDate &&
            x.MealType == mealType &&
            x.PersonAssignments.Select(a => a.PersonId).Intersect(assignments.Select(a => a.PersonId)).Any());

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"One or more persons already have a planned meal for {plannedDate:yyyy-MM-dd} as {mealType}.");
        }

        var entry = new MealPlanEntry(Id, baseRecipe.Id, plannedDate, mealType, scope);
        entry.SetSaladRecipe(saladRecipeId);

        foreach (var assignment in assignments)
        {
            entry.AssignPerson(
                assignment.PersonId,
                assignment.AssignedRecipeId,
                assignment.RecipeVariationId,
                assignment.PortionMultiplier,
                assignment.Notes);
        }

        _entries.Add(entry);

        RaiseDomainEvent(new MealPlanRecipeAdded(
            Id,
            entry.Id,
            entry.BaseRecipeId,
            entry.PlannedDate,
            entry.MealType));
    }

    public void SwapEntries(MealPlanEntryId entryAId, MealPlanEntryId entryBId)
    {
        var a = _entries.SingleOrDefault(x => x.Id == entryAId)
                ?? throw new InvalidOperationException($"Meal plan entry '{entryAId}' was not found.");
        var b = _entries.SingleOrDefault(x => x.Id == entryBId)
                ?? throw new InvalidOperationException($"Meal plan entry '{entryBId}' was not found.");

        var (dateA, typeA) = (a.PlannedDate, a.MealType);
        a.SetSlot(b.PlannedDate, b.MealType);
        b.SetSlot(dateA, typeA);
    }

    public void RemoveEntry(MealPlanEntryId entryId)
    {
        var entry = _entries.SingleOrDefault(x => x.Id == entryId)
                   ?? throw new InvalidOperationException($"Meal plan entry '{entryId}' was not found.");

        _entries.Remove(entry);

        RaiseDomainEvent(new MealPlanEntryRemoved(
            Id,
            entry.Id,
            entry.BaseRecipeId,
            entry.PlannedDate,
            entry.MealType));
    }

    public void UpdatePersonAssignment(
        MealPlanEntryId entryId,
        PersonId personId,
        RecipeId assignedRecipeId,
        RecipeVariationId? recipeVariationId,
        decimal portionMultiplier,
        string? notes = null)
    {
        var entry = _entries.SingleOrDefault(x => x.Id == entryId)
                ?? throw new InvalidOperationException($"Meal plan entry '{entryId}' was not found.");

        entry.UpdatePersonAssignment(
            personId,
            assignedRecipeId,
            recipeVariationId,
            portionMultiplier,
            notes);
    }
}