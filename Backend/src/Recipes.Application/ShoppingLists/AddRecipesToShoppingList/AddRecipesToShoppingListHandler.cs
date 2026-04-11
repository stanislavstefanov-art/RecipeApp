using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.ShoppingLists.AddRecipesToShoppingList;

public sealed class AddRecipesToShoppingListHandler
    : IRequestHandler<AddRecipesToShoppingListCommand, ErrorOr<Success>>
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IProductRepository _productRepository;

    public AddRecipesToShoppingListHandler(
        IShoppingListRepository shoppingListRepository,
        IRecipeRepository recipeRepository,
        IProductRepository productRepository)
    {
        _shoppingListRepository = shoppingListRepository;
        _recipeRepository = recipeRepository;
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        AddRecipesToShoppingListCommand request,
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

        foreach (var recipeGuid in request.RecipeIds.Distinct())
        {
            var recipeId = RecipeId.From(recipeGuid);
            var recipe = await _recipeRepository.GetByIdAsync(recipeId, cancellationToken);

            if (recipe is null)
            {
                return Error.NotFound(
                    "Recipe.NotFound",
                    $"Recipe '{recipeGuid}' was not found.");
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
        }

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}