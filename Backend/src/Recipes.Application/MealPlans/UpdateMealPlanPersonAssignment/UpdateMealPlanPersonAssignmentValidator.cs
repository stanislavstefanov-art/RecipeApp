using FluentValidation;

namespace Recipes.Application.MealPlans.UpdateMealPlanPersonAssignment;

public sealed class UpdateMealPlanPersonAssignmentValidator
    : AbstractValidator<UpdateMealPlanPersonAssignmentCommand>
{
    public UpdateMealPlanPersonAssignmentValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.MealPlanEntryId).NotEmpty();
        RuleFor(x => x.PersonId).NotEmpty();
        RuleFor(x => x.AssignedRecipeId).NotEmpty();
        RuleFor(x => x.PortionMultiplier).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}