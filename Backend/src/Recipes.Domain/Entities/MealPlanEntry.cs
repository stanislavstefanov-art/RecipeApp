namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;

public sealed class MealPlanEntry
{
    private readonly List<MealPlanPersonAssignment> _personAssignments = [];

    public MealPlanEntryId Id { get; private set; } = MealPlanEntryId.New();
    public MealPlanId MealPlanId { get; private set; }
    public RecipeId BaseRecipeId { get; private set; }
    public DateOnly PlannedDate { get; private set; }
    public MealType MealType { get; private set; }
    public MealScope Scope { get; private set; }
    public IReadOnlyCollection<MealPlanPersonAssignment> PersonAssignments => _personAssignments.AsReadOnly();

    private MealPlanEntry() { }

    internal MealPlanEntry(
        MealPlanId mealPlanId,
        RecipeId baseRecipeId,
        DateOnly plannedDate,
        MealType mealType,
        MealScope scope)
    {
        MealPlanId = mealPlanId;
        BaseRecipeId = baseRecipeId;
        PlannedDate = plannedDate;
        MealType = mealType;
        Scope = scope;
    }

    internal void AssignPerson(
        PersonId personId,
        RecipeId assignedRecipeId,
        RecipeVariationId? recipeVariationId,
        decimal portionMultiplier,
        string? notes = null)
    {
        var existing = _personAssignments.SingleOrDefault(x => x.PersonId == personId);

        if (existing is not null)
        {
            existing.ChangeAssignedRecipe(assignedRecipeId, recipeVariationId, notes);
            existing.ChangePortionMultiplier(portionMultiplier);
            return;
        }

        _personAssignments.Add(new MealPlanPersonAssignment(
            Id,
            personId,
            assignedRecipeId,
            recipeVariationId,
            portionMultiplier,
            notes));
    }

    internal void UpdatePersonAssignment(
        PersonId personId,
        RecipeId assignedRecipeId,
        RecipeVariationId? recipeVariationId,
        decimal portionMultiplier,
        string? notes = null)
    {
        var existing = _personAssignments.SingleOrDefault(x => x.PersonId == personId)
                    ?? throw new InvalidOperationException($"Person '{personId}' is not assigned to this meal plan entry.");

        existing.ChangeAssignedRecipe(assignedRecipeId, recipeVariationId, notes);
        existing.ChangePortionMultiplier(portionMultiplier);
        existing.UpdateNotes(notes);
    }
}