using FluentValidation;

namespace Recipes.Application.Recipes.RemoveStepFromRecipe;

public sealed class RemoveStepFromRecipeValidator : AbstractValidator<RemoveStepFromRecipeCommand>
{
    public RemoveStepFromRecipeValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
    }
}
