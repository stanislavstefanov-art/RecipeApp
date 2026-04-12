using FluentValidation;

namespace Recipes.Application.Recipes.AddRecipeVariation;

public sealed class AddRecipeVariationValidator : AbstractValidator<AddRecipeVariationCommand>
{
    public AddRecipeVariationValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.IngredientAdjustmentNotes).MaximumLength(2000);
    }
}