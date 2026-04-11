using FluentValidation;

namespace Recipes.Application.ShoppingLists.MarkShoppingListItemPurchased;

public sealed class MarkShoppingListItemPurchasedValidator : AbstractValidator<MarkShoppingListItemPurchasedCommand>
{
    public MarkShoppingListItemPurchasedValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.ShoppingListItemId).NotEmpty();
    }
}