using FluentValidation;

namespace Recipes.Application.ShoppingLists.AddManualItemToShoppingList;

public sealed class AddManualItemToShoppingListValidator : AbstractValidator<AddManualItemToShoppingListCommand>
{
    public AddManualItemToShoppingListValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
    }
}
