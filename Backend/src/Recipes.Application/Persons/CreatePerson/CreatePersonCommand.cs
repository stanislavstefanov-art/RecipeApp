using ErrorOr;
using MediatR;

namespace Recipes.Application.Persons.CreatePerson;

public sealed record CreatePersonCommand(
    string Name,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes,
    Guid HouseholdId,
    DateOnly? DateOfBirth = null,
    int? Gender = null) : IRequest<ErrorOr<CreatePersonResponse>>;