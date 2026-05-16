using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.DeleteShoppingList;

public sealed class DeleteShoppingListHandler : IRequestHandler<DeleteShoppingListCommand, ErrorOr<Deleted>>
{
    private readonly IShoppingListRepository _repository;

    public DeleteShoppingListHandler(IShoppingListRepository repository) => _repository = repository;

    public async Task<ErrorOr<Deleted>> Handle(DeleteShoppingListCommand request, CancellationToken cancellationToken)
    {
        var id = ShoppingListId.From(request.Id);
        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity is null)
            return Error.NotFound("ShoppingList.NotFound", $"Shopping list '{request.Id}' was not found.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
