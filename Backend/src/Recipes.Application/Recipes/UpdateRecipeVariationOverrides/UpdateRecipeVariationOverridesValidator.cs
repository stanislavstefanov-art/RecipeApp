using FluentValidation;

namespace Recipes.Application.Recipes.UpdateRecipeVariationOverrides;

public sealed class UpdateRecipeVariationOverridesValidator
    : AbstractValidator<UpdateRecipeVariationOverridesCommand>
{
    public UpdateRecipeVariationOverridesValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.RecipeVariationId).NotEmpty();
        RuleForEach(x => x.Overrides).ChildRules(o =>
        {
            o.RuleFor(x => x.IngredientName).NotEmpty().MaximumLength(200);
            o.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .When(x => x.Quantity.HasValue && !x.IsRemoved);
            o.RuleFor(x => x.Unit)
                .NotEmpty()
                .When(x => x.Quantity.HasValue && !x.IsRemoved);
        });
    }
}