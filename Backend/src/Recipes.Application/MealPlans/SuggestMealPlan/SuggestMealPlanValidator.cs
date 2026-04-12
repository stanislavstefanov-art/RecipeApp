using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed class SuggestMealPlanValidator : AbstractValidator<SuggestMealPlanCommand>
{
    public SuggestMealPlanValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.HouseholdId)
            .NotEmpty();

        RuleFor(x => x.NumberOfDays)
            .GreaterThan(0)
            .LessThanOrEqualTo(31);

        RuleFor(x => x.MealTypes)
            .NotEmpty();

        RuleForEach(x => x.MealTypes)
            .Must(x => Enum.IsDefined(typeof(MealType), x));
    }
}