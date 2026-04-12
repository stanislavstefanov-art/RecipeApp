using FluentValidation;

namespace Recipes.Application.Households.CreateHousehold;

public sealed class CreateHouseholdValidator : AbstractValidator<CreateHouseholdCommand>
{
    public CreateHouseholdValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}