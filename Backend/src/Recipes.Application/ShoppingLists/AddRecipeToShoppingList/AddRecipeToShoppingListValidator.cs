using FluentValidation;

namespace Recipes.Application.ShoppingLists.AddRecipeToShoppingList;

public sealed class AddRecipeToShoppingListValidator : AbstractValidator<AddRecipeToShoppingListCommand>
{
    public AddRecipeToShoppingListValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.RecipeId).NotEmpty();
    }
}