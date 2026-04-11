using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.MealPlans.AcceptMealPlanSuggestion;

public sealed class AcceptMealPlanSuggestionValidator : AbstractValidator<AcceptMealPlanSuggestionCommand>
{
    public AcceptMealPlanSuggestionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Entries)
            .NotEmpty();

        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(x => x.RecipeId).NotEmpty();
            entry.RuleFor(x => x.MealType)
                .Must(x => Enum.IsDefined(typeof(MealType), x));
        });
    }
}