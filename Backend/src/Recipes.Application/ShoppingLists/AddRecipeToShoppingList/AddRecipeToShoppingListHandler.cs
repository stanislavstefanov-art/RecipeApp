using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.AddRecipeToShoppingList;

public sealed class AddRecipeToShoppingListHandler
    : IRequestHandler<AddRecipeToShoppingListCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUser _currentUser;

    public AddRecipeToShoppingListHandler(
        IShoppingListRepository shoppingListRepository,
        IRecipeRepository recipeRepository,
        IProductRepository productRepository,
        ICurrentUser currentUser)
    {
        _shoppingListRepository = shoppingListRepository;
        _recipeRepository = recipeRepository;
        _productRepository = productRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddRecipeToShoppingListCommand request,
        CancellationToken cancellationToken)
    {
        var shoppingListId = ShoppingListId.From(request.ShoppingListId);
        var recipeId = RecipeId.From(request.RecipeId);

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        if (shoppingList is null)
        {
            return Error.NotFound(
                code: "ShoppingList.NotFound",
                description: $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        if (shoppingList.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(shoppingList.HouseholdId.Value))
            {
                return Error.NotFound(
                    code: "ShoppingList.NotFound",
                    description: $"Shopping list '{request.ShoppingListId}' was not found.");
            }
        }

        var recipe = await _recipeRepository.GetByIdAsync(recipeId, cancellationToken);
        if (recipe is null)
        {
            return Error.NotFound(
                code: "Recipe.NotFound",
                description: $"Recipe '{request.RecipeId}' was not found.");
        }

        foreach (var ingredient in recipe.Ingredients)
        {
            var product = await _productRepository.GetByNameAsync(ingredient.Name, cancellationToken);

            if (product is null)
            {
                product = new Product(ingredient.Name);
                await _productRepository.AddAsync(product, cancellationToken);
            }

            shoppingList.AddItem(product, ingredient.Quantity, ingredient.Unit);
        }

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}