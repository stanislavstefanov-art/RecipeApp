using FluentValidation;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public sealed class ImportRecipeFromUrlValidator : AbstractValidator<ImportRecipeFromUrlCommand>
{
    public ImportRecipeFromUrlValidator()
    {
        RuleFor(x => x.SourceUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                         && u.Scheme is "http" or "https")
            .WithMessage("SourceUrl must be an absolute http or https URL.");
    }
}
