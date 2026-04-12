using FluentValidation;

namespace Recipes.Application.Persons.GetPerson;

public sealed class GetPersonValidator : AbstractValidator<GetPersonQuery>
{
    public GetPersonValidator()
    {
        RuleFor(x => x.PersonId).NotEmpty();
    }
}