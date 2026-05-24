using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.AddManualItemToShoppingList;

public sealed class AddManualItemToShoppingListHandler
    : IRequestHandler<AddManualItemToShoppingListCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUser _currentUser;

    public AddManualItemToShoppingListHandler(
        IShoppingListRepository shoppingListRepository,
        IProductRepository productRepository,
        ICurrentUser currentUser)
    {
        _shoppingListRepository = shoppingListRepository;
        _productRepository = productRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddManualItemToShoppingListCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingListId = ShoppingListId.From(request.ShoppingListId);

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
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

        var product = await _productRepository.GetByNameAsync(request.ProductName, cancellationToken);
        if (product is null)
        {
            product = new Product(request.ProductName);
            await _productRepository.AddAsync(product, cancellationToken);
        }

        shoppingList.AddItem(product, request.Quantity, request.Unit, sourceType: ShoppingListItemSourceType.Manual);

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
