using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.GetShoppingList;

public sealed class GetShoppingListHandler
    : IRequestHandler<GetShoppingListQuery, ErrorOr<ShoppingListDetailsDto>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly ICurrentUser _currentUser;

    public GetShoppingListHandler(IShoppingListRepository shoppingListRepository, ICurrentUser currentUser)
    {
        _shoppingListRepository = shoppingListRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<ShoppingListDetailsDto>> Handle(
        GetShoppingListQuery request,
        CancellationToken cancellationToken)
    {
        var id = ShoppingListId.From(request.ShoppingListId);

        var shoppingList = await _shoppingListRepository.GetByIdAsync(id, cancellationToken);
        if (shoppingList is null)
        {
            return Error.NotFound(
                "ShoppingList.NotFound",
                $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        if (shoppingList.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(shoppingList.HouseholdId.Value))
            {
                return Error.NotFound(
                    "ShoppingList.NotFound",
                    $"Shopping list '{request.ShoppingListId}' was not found.");
            }
        }

        return new ShoppingListDetailsDto(
            shoppingList.Id.Value,
            shoppingList.Name,
            shoppingList.Items.Select(i => new ShoppingListDetailsItemDto(
                i.Id.Value,
                i.ProductId.Value,
                i.ProductName,
                i.Quantity,
                i.Unit,
                i.IsPurchased,
                i.Notes,
                (int)i.SourceType,
                i.SourceReferenceId,
                i.RecipeSources.Select(s => new ShoppingListItemRecipeSourceDto(s.RecipeId.Value, s.RecipeName, s.Portions)).ToList())).ToList());
    }
}