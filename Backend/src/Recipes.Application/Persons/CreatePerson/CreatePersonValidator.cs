using FluentValidation;
using Recipes.Domain.Enums;
using Gender = Recipes.Domain.Enums.Gender;

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
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.DateOfBirth)
            .Must(d => d == null || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.")
            .Must(d => d == null || d.Value.Year >= 1900)
            .WithMessage("Date of birth is not plausible.");
        RuleFor(x => x.Gender)
            .Must(g => g == null || Enum.IsDefined(typeof(Gender), g))
            .WithMessage("Invalid gender value.");
    }
}