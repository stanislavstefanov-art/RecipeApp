using ErrorOr;
using MediatR;

namespace Recipes.Application.Persons.ListPersons;

public sealed record ListPersonsQuery() : IRequest<ErrorOr<IReadOnlyList<PersonDto>>>;