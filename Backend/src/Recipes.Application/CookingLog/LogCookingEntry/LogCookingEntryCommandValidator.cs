using FluentValidation;

namespace Recipes.Application.CookingLog.LogCookingEntry;

public sealed class LogCookingEntryCommandValidator : AbstractValidator<LogCookingEntryCommand>
{
    public LogCookingEntryCommandValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.CookedOn)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Cooked date cannot be in the future.");
        RuleFor(x => x.Servings).InclusiveBetween(1, 100);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
