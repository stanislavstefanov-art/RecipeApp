using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Pantry.AddPantryItem;

public sealed class AddPantryItemHandler : IRequestHandler<AddPantryItemCommand, ErrorOr<PantryItemDto>>
{
    private readonly IPantryRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public AddPantryItemHandler(IPantryRepository repository, ICurrentUser currentUser, TimeProvider time)
    {
        _repository = repository;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ErrorOr<PantryItemDto>> Handle(AddPantryItemCommand request, CancellationToken cancellationToken)
    {
        var item = new PantryItem(_currentUser.UserId, request.IngredientName, request.Notes, _time.GetUtcNow());
        await _repository.AddAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return new PantryItemDto(item.Id.Value, item.IngredientName, item.Notes, item.CreatedAt);
    }
}
