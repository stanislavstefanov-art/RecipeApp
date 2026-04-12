using FluentValidation;

namespace Recipes.Application.Households.AddPersonToHousehold;

public sealed class AddPersonToHouseholdValidator : AbstractValidator<AddPersonToHouseholdCommand>
{
    public AddPersonToHouseholdValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.PersonId).NotEmpty();
    }
}