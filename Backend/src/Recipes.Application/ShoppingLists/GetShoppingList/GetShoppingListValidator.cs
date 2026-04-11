using FluentValidation;

namespace Recipes.Application.ShoppingLists.GetShoppingList;

public sealed class GetShoppingListValidator : AbstractValidator<GetShoppingListQuery>
{
    public GetShoppingListValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
    }
}