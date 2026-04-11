using FluentValidation;

namespace Recipes.Application.ShoppingLists.AddItemToShoppingList;

public sealed class AddItemToShoppingListValidator : AbstractValidator<AddItemToShoppingListCommand>
{
    public AddItemToShoppingListValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
    }
}