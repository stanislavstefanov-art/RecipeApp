using FluentValidation;

namespace Recipes.Application.Recipes.ScaleRecipe;

public sealed class ScaleRecipeValidator : AbstractValidator<ScaleRecipeCommand>
{
    public ScaleRecipeValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty();

        RuleFor(x => x.FromServings)
            .GreaterThan(0);

        RuleFor(x => x.ToServings)
            .GreaterThan(0);
    }
}
