namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class MealPlanPersonAssignment
{
    public MealPlanPersonAssignmentId Id { get; private set; } = MealPlanPersonAssignmentId.New();
    public MealPlanEntryId MealPlanEntryId { get; private set; }
    public PersonId PersonId { get; private set; }
    public RecipeId AssignedRecipeId { get; private set; }
    public RecipeVariationId? RecipeVariationId { get; private set; }
    public decimal PortionMultiplier { get; private set; }
    public string? Notes { get; private set; }

    private MealPlanPersonAssignment() { }

    internal MealPlanPersonAssignment(
        MealPlanEntryId mealPlanEntryId,
        PersonId personId,
        RecipeId assignedRecipeId,
        RecipeVariationId? recipeVariationId,
        decimal portionMultiplier,
        string? notes = null)
    {
        if (portionMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(portionMultiplier), "Portion multiplier must be greater than zero.");
        }

        MealPlanEntryId = mealPlanEntryId;
        PersonId = personId;
        AssignedRecipeId = assignedRecipeId;
        RecipeVariationId = recipeVariationId;
        PortionMultiplier = decimal.Round(portionMultiplier, 2, MidpointRounding.AwayFromZero);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    internal void ChangeAssignedRecipe(
        RecipeId recipeId,
        RecipeVariationId? recipeVariationId,
        string? notes = null)
    {
        AssignedRecipeId = recipeId;
        RecipeVariationId = recipeVariationId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    internal void ChangePortionMultiplier(decimal portionMultiplier)
    {
        if (portionMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(portionMultiplier), "Portion multiplier must be greater than zero.");
        }

        PortionMultiplier = decimal.Round(portionMultiplier, 2, MidpointRounding.AwayFromZero);
    }

    internal void UpdateNotes(string? notes)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}