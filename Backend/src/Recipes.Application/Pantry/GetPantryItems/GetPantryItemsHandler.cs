using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Pantry.GetPantryItems;

public sealed class GetPantryItemsHandler : IRequestHandler<GetPantryItemsQuery, ErrorOr<IReadOnlyList<PantryItemDto>>>
{
    private readonly IPantryRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetPantryItemsHandler(IPantryRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<PantryItemDto>>> Handle(GetPantryItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByUserAsync(_currentUser.UserId, cancellationToken);
        return items.Select(i => new PantryItemDto(i.Id.Value, i.IngredientName, i.Notes, i.CreatedAt))
                    .ToList()
                    .ToErrorOr<IReadOnlyList<PantryItemDto>>();
    }
}
