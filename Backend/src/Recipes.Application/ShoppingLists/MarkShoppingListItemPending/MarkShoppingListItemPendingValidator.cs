using FluentValidation;

namespace Recipes.Application.ShoppingLists.MarkShoppingListItemPending;

public sealed class MarkShoppingListItemPendingValidator : AbstractValidator<MarkShoppingListItemPendingCommand>
{
    public MarkShoppingListItemPendingValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.ShoppingListItemId).NotEmpty();
    }
}