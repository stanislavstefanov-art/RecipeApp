using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.ListShoppingLists;

public sealed class ListShoppingListsHandler
    : IRequestHandler<ListShoppingListsQuery, ErrorOr<IReadOnlyList<ShoppingListDto>>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly ICurrentUser _currentUser;

    public ListShoppingListsHandler(IShoppingListRepository shoppingListRepository, ICurrentUser currentUser)
    {
        _shoppingListRepository = shoppingListRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<ShoppingListDto>>> Handle(
        ListShoppingListsQuery request,
        CancellationToken cancellationToken)
    {
        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
        var shoppingLists = await _shoppingListRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);

        var result = shoppingLists
            .Select(x => new ShoppingListDto(
                x.Id.Value,
                x.Name,
                x.Items.Select(i => new ShoppingListItemDto(
                    i.Id.Value,
                    i.ProductId.Value,
                    i.ProductName,
                    i.Quantity,
                    i.Unit,
                    i.IsPurchased,
                    i.Notes,
                    (int)i.SourceType,
                    i.SourceReferenceId))
                .ToList()))
            .ToList();

        return result;
    }
}