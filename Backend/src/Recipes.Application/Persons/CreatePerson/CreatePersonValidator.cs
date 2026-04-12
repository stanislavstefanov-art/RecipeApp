using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.Persons.CreatePerson;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.DietaryPreferences)
            .Must(x => Enum.IsDefined(typeof(DietaryPreference), x));
        RuleForEach(x => x.HealthConcerns)
            .Must(x => Enum.IsDefined(typeof(HealthConcern), x));
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}