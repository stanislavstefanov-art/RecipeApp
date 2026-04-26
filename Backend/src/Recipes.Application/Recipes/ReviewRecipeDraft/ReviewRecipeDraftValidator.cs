using FluentValidation;

namespace Recipes.Application.Recipes.ReviewRecipeDraft;

public sealed class ReviewRecipeDraftValidator : AbstractValidator<ReviewRecipeDraftCommand>
{
    public ReviewRecipeDraftValidator()
    {
        RuleFor(x => x.RecipeText)
            .NotEmpty()
            .MaximumLength(8000);
    }
}
