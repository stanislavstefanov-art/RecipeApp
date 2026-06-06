using FluentValidation;

namespace Recipes.Application.Recipes.SetRecipeOrigin;

public sealed class SetRecipeOriginValidator : AbstractValidator<SetRecipeOriginCommand>
{
    private static readonly int[] ValidOrigins = [1, 2];

    public SetRecipeOriginValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();

        RuleFor(x => x.Origin)
            .Must(v => ValidOrigins.Contains(v))
            .WithMessage("Origin must be 1 (Home) or 2 (Borrowed).");
    }
}
