using FluentValidation;

namespace Recipes.Application.Recipes.SetRecipeDifficulty;

public sealed class SetRecipeDifficultyValidator : AbstractValidator<SetRecipeDifficultyCommand>
{
    private static readonly int[] ValidLevels = [1, 2, 3];

    public SetRecipeDifficultyValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();

        RuleFor(x => x.DifficultyLevel)
            .Must(v => v is null || ValidLevels.Contains(v.Value))
            .WithMessage("DifficultyLevel must be 1 (Easy), 2 (Medium), or 3 (Hard), or null to clear.");
    }
}
