using FluentValidation;

namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public sealed class IngredientSubstitutionSuggestionDtoValidator
    : AbstractValidator<IngredientSubstitutionSuggestionDto>
{
    public IngredientSubstitutionSuggestionDtoValidator()
    {
        RuleFor(x => x.OriginalIngredient)
            .NotEmpty();

        RuleFor(x => x.Substitutes)
            .NotEmpty();

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0, 1);

        RuleForEach(x => x.Substitutes).ChildRules(sub =>
        {
            sub.RuleFor(x => x.Name).NotEmpty();
            sub.RuleFor(x => x.Reason).NotEmpty();
            sub.RuleFor(x => x.QuantityAdjustment).MaximumLength(200);
        });
    }
}