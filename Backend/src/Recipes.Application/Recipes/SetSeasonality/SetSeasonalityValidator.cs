using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.Recipes.SetSeasonality;

public sealed class SetSeasonalityValidator : AbstractValidator<SetSeasonalityCommand>
{
    public SetSeasonalityValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Seasonality)
            .Must(v => Enum.IsDefined(typeof(Season), v))
            .WithMessage("Invalid seasonality value.");
    }
}
