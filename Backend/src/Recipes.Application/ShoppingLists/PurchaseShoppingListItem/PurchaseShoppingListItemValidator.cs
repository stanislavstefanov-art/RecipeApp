using FluentValidation;

namespace Recipes.Application.ShoppingLists.PurchaseShoppingListItem;

public sealed class PurchaseShoppingListItemValidator : AbstractValidator<PurchaseShoppingListItemCommand>
{
    public PurchaseShoppingListItemValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.ShoppingListItemId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}