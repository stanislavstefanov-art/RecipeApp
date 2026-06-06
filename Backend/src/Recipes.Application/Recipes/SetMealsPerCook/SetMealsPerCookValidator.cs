using FluentValidation;

namespace Recipes.Application.Recipes.SetMealsPerCook;

public sealed class SetMealsPerCookValidator : AbstractValidator<SetMealsPerCookCommand>
{
    public SetMealsPerCookValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.MealsPerCook).InclusiveBetween(1, 2)
            .WithMessage("MealsPerCook must be 1 or 2.");
    }
}
