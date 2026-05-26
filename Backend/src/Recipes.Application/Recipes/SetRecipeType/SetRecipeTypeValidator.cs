using FluentValidation;

namespace Recipes.Application.Recipes.SetRecipeType;

public sealed class SetRecipeTypeValidator : AbstractValidator<SetRecipeTypeCommand>
{
    private static readonly int[] ValidTypes = [1, 2, 3];

    public SetRecipeTypeValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();

        RuleFor(x => x.RecipeType)
            .Must(v => ValidTypes.Contains(v))
            .WithMessage("RecipeType must be 1 (Main dish), 2 (Salad), or 3 (Appetizer).");
    }
}
