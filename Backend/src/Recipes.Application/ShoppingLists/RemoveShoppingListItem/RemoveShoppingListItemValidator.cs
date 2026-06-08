using FluentValidation;

namespace Recipes.Application.ShoppingLists.RemoveShoppingListItem;

public sealed class RemoveShoppingListItemValidator : AbstractValidator<RemoveShoppingListItemCommand>
{
    public RemoveShoppingListItemValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.ShoppingListItemId).NotEmpty();
    }
}
