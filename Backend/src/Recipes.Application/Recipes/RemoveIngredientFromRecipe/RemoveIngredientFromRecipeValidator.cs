using FluentValidation;

namespace Recipes.Application.Recipes.RemoveIngredientFromRecipe;

public sealed class RemoveIngredientFromRecipeValidator : AbstractValidator<RemoveIngredientFromRecipeCommand>
{
    public RemoveIngredientFromRecipeValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.IngredientId).NotEmpty();
    }
}
