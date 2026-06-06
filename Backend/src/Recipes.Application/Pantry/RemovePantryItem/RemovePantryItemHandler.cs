using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Pantry.RemovePantryItem;

public sealed class RemovePantryItemHandler : IRequestHandler<RemovePantryItemCommand, ErrorOr<Success>>
{
    private readonly IPantryRepository _repository;
    private readonly ICurrentUser _currentUser;

    public RemovePantryItemHandler(IPantryRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Success>> Handle(RemovePantryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(PantryItemId.From(request.Id), cancellationToken);
        if (item is null || item.UserId != _currentUser.UserId)
            return Error.NotFound("PantryItem.NotFound", $"Pantry item '{request.Id}' was not found.");

        _repository.Remove(item);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
