using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.Recipes.SetAppropriateForMealTypes;

public sealed class SetAppropriateForMealTypesValidator : AbstractValidator<SetAppropriateForMealTypesCommand>
{
    public SetAppropriateForMealTypesValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.MealTypes).NotNull();
        RuleForEach(x => x.MealTypes)
            .Must(v => Enum.IsDefined(typeof(MealType), v))
            .WithMessage("Invalid meal type value.");
    }
}
