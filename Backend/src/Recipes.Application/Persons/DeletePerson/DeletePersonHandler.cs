using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Persons.DeletePerson;

public sealed class DeletePersonHandler : IRequestHandler<DeletePersonCommand, ErrorOr<Deleted>>
{
    private readonly IPersonRepository _repository;

    public DeletePersonHandler(IPersonRepository repository) => _repository = repository;

    public async Task<ErrorOr<Deleted>> Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        var id = PersonId.From(request.Id);
        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity is null)
            return Error.NotFound("Person.NotFound", $"Person '{request.Id}' was not found.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
