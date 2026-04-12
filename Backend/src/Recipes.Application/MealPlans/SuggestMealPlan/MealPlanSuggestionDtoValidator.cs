using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.MealPlans.SuggestMealPlan;

public sealed class MealPlanSuggestionDtoValidator : AbstractValidator<MealPlanSuggestionDto>
{
    public MealPlanSuggestionDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty();
        RuleFor(x => x.Confidence).InclusiveBetween(0, 1);

        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(x => x.BaseRecipeId).NotEmpty();

            entry.RuleFor(x => x.MealType)
                .Must(x => Enum.IsDefined(typeof(MealType), x));

            entry.RuleFor(x => x.Scope)
                .Must(x => Enum.IsDefined(typeof(MealScope), x));

            entry.RuleFor(x => x.Assignments)
                .NotEmpty();

            entry.RuleForEach(x => x.Assignments).ChildRules(a =>
            {
                a.RuleFor(x => x.PersonId).NotEmpty();
                a.RuleFor(x => x.AssignedRecipeId).NotEmpty();
                a.RuleFor(x => x.PortionMultiplier).GreaterThan(0);
                a.RuleFor(x => x.Notes).MaximumLength(1000);
            });
        });
    }
}