using FluentValidation;

namespace Recipes.Application.Recipes.ImportRecipeFromText;

public sealed class ImportedRecipeDtoValidator : AbstractValidator<ImportedRecipeDto>
{
    public ImportedRecipeDtoValidator()
    {
        RuleFor(x => x.Ingredients)
            .NotEmpty();

        RuleFor(x => x.Steps)
            .NotEmpty();

        RuleForEach(x => x.Ingredients).ChildRules(ingredient =>
        {
            ingredient.RuleFor(i => i.Name).NotEmpty();
        });

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0, 1);
    }
}