using ErrorOr;
using MediatR;

namespace Recipes.Application.Persons.DeletePerson;

public sealed record DeletePersonCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
