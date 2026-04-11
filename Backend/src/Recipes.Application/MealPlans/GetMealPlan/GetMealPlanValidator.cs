using FluentValidation;

namespace Recipes.Application.MealPlans.GetMealPlan;

public sealed class GetMealPlanValidator : AbstractValidator<GetMealPlanQuery>
{
    public GetMealPlanValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
    }
}