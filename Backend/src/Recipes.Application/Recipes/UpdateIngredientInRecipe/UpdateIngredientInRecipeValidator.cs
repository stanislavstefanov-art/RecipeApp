using FluentValidation;

namespace Recipes.Application.Recipes.UpdateIngredientInRecipe;

public sealed class UpdateIngredientInRecipeValidator : AbstractValidator<UpdateIngredientInRecipeCommand>
{
    public UpdateIngredientInRecipeValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.IngredientId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Unit)
            .MaximumLength(50);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}
