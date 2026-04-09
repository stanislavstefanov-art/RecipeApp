using FluentValidation;

namespace Recipes.Application.Recipes.SearchRecipesByIngredient;

public sealed class SearchRecipesByIngredientValidator : AbstractValidator<SearchRecipesByIngredientQuery>
{
    public SearchRecipesByIngredientValidator()
    {
        RuleFor(x => x.Ingredient)
            .NotEmpty()
            .MaximumLength(200);
    }
}

