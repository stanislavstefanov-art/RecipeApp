using FluentValidation;

namespace Recipes.Application.Pantry.RemovePantryItem;

public sealed class RemovePantryItemValidator : AbstractValidator<RemovePantryItemCommand>
{
    public RemovePantryItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
