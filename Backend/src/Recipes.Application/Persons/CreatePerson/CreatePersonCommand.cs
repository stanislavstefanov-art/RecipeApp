using ErrorOr;
using MediatR;

namespace Recipes.Application.Persons.CreatePerson;

public sealed record CreatePersonCommand(
    string Name,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes) : IRequest<ErrorOr<CreatePersonResponse>>;