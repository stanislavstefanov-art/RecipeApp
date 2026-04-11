using FluentValidation;

namespace Recipes.Application.ShoppingLists.AddRecipesToShoppingList;

public sealed class AddRecipesToShoppingListValidator : AbstractValidator<AddRecipesToShoppingListCommand>
{
    public AddRecipesToShoppingListValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.RecipeIds).NotEmpty();
        RuleForEach(x => x.RecipeIds).NotEmpty();
    }
}