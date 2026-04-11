using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.AddItemToShoppingList;

public sealed class AddItemToShoppingListHandler
    : IRequestHandler<AddItemToShoppingListCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IProductRepository _productRepository;

    public AddItemToShoppingListHandler(
        IShoppingListRepository shoppingListRepository,
        IProductRepository productRepository)
    {
        _shoppingListRepository = shoppingListRepository;
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddItemToShoppingListCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingListId = ShoppingListId.From(request.ShoppingListId);
        var productId = ProductId.From(request.ProductId);

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        if (shoppingList is null)
        {
            return Error.NotFound(
                code: "ShoppingList.NotFound",
                description: $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Error.NotFound(
                code: "Product.NotFound",
                description: $"Product '{request.ProductId}' was not found.");
        }

        shoppingList.AddItem(product, request.Quantity, request.Unit);

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}