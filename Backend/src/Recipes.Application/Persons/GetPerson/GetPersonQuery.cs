using ErrorOr;
using MediatR;

namespace Recipes.Application.Persons.GetPerson;

public sealed record GetPersonQuery(Guid PersonId) : IRequest<ErrorOr<PersonDetailsDto>>;