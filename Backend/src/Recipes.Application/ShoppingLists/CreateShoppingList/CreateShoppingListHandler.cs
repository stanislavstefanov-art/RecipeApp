using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.CreateShoppingList;

public sealed class CreateShoppingListHandler
    : IRequestHandler<CreateShoppingListCommand, ErrorOr<CreateShoppingListResponse>>
{
    private readonly IShoppingListRepository _shoppingListRepository;

    public CreateShoppingListHandler(IShoppingListRepository shoppingListRepository)
    {
        _shoppingListRepository = shoppingListRepository;
    }

    public async Task<ErrorOr<CreateShoppingListResponse>> Handle(
        CreateShoppingListCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingList = new ShoppingList(request.Name);

        await _shoppingListRepository.AddAsync(shoppingList, cancellationToken);
        await _shoppingListRepository.SaveChangesAsync(cancellationToken);

        return new CreateShoppingListResponse(
            shoppingList.Id.Value,
            shoppingList.Name);
    }
}