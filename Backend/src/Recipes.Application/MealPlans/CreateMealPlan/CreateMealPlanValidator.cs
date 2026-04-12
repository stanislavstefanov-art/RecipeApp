using FluentValidation;

namespace Recipes.Application.MealPlans.CreateMealPlan;

public sealed class CreateMealPlanValidator : AbstractValidator<CreateMealPlanCommand>
{
    public CreateMealPlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.HouseholdId).NotEmpty();
    }
}