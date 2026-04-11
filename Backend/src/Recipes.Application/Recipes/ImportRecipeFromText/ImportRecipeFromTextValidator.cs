using FluentValidation;

namespace Recipes.Application.Recipes.ImportRecipeFromText;

public sealed class ImportRecipeFromTextValidator : AbstractValidator<ImportRecipeFromTextCommand>
{
    public ImportRecipeFromTextValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MinimumLength(10);
    }
}