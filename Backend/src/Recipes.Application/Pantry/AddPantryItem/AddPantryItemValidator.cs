using FluentValidation;

namespace Recipes.Application.Pantry.AddPantryItem;

public sealed class AddPantryItemValidator : AbstractValidator<AddPantryItemCommand>
{
    public AddPantryItemValidator()
    {
        RuleFor(x => x.IngredientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
