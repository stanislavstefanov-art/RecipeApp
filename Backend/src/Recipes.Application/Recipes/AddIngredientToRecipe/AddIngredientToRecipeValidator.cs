using FluentValidation;

namespace Recipes.Application.Recipes.AddIngredientToRecipe;

public sealed class AddIngredientToRecipeValidator : AbstractValidator<AddIngredientToRecipeCommand>
{
    public AddIngredientToRecipeValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}

