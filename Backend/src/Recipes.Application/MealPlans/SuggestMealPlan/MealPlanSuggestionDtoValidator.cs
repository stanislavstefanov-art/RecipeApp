using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed class MealPlanSuggestionDtoValidator : AbstractValidator<MealPlanSuggestionDto>
{
    public MealPlanSuggestionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Entries)
            .NotEmpty();

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0, 1);

        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(x => x.RecipeId).NotEmpty();
            entry.RuleFor(x => x.MealType)
                .Must(x => Enum.IsDefined(typeof(MealType), x));
        });
    }
}