using FluentValidation;

namespace Recipes.Application.Recipes.CritiqueRecipe;

public sealed class CritiqueRecipeValidator : AbstractValidator<CritiqueRecipeCommand>
{
    public CritiqueRecipeValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty();
    }
}
