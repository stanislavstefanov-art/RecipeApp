using FluentValidation;

namespace Recipes.Application.ShoppingLists.CreateShoppingList;

public sealed class CreateShoppingListValidator : AbstractValidator<CreateShoppingListCommand>
{
    public CreateShoppingListValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}