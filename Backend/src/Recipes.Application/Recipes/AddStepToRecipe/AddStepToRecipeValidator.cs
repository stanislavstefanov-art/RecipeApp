using FluentValidation;

namespace Recipes.Application.Recipes.AddStepToRecipe;

public sealed class AddStepToRecipeValidator : AbstractValidator<AddStepToRecipeCommand>
{
    public AddStepToRecipeValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty();

        RuleFor(x => x.Instruction)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
