using FluentValidation;

namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public sealed class SuggestIngredientSubstitutionsValidator : AbstractValidator<SuggestIngredientSubstitutionsCommand>
{
    public SuggestIngredientSubstitutionsValidator()
    {
        RuleFor(x => x.IngredientName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.RecipeContext)
            .MaximumLength(2000);

        RuleFor(x => x.DietaryGoal)
            .MaximumLength(200);
    }
}