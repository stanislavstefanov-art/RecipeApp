using FluentValidation;

namespace Recipes.Application.Recipes.RateRecipe;

public sealed class RateRecipeCommandValidator : AbstractValidator<RateRecipeCommand>
{
    public RateRecipeCommandValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment is not null);
    }
}
