using FluentValidation;

namespace Recipes.Application.Households.GetHousehold;

public sealed class GetHouseholdValidator : AbstractValidator<GetHouseholdQuery>
{
    public GetHouseholdValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
    }
}