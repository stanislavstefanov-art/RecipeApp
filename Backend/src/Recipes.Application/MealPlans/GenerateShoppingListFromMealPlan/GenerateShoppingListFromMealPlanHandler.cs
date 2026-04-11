using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.GenerateShoppingListFromMealPlan;

public sealed class GenerateShoppingListFromMealPlanHandler
    : IRequestHandler<GenerateShoppingListFromMealPlanCommand, ErrorOr<Success>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IProductRepository _productRepository;

    public GenerateShoppingListFromMealPlanHandler(
        IMealPlanRepository mealPlanRepository,
        IShoppingListRepository shoppingListRepository,
        IRecipeRepository recipeRepository,
        IProductRepository productRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _shoppingListRepository = shoppingListRepository;
        _recipeRepository = recipeRepository;
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        GenerateShoppingListFromMealPlanCommand request,
        CancellationToken cancellationToken)
    {
        var mealPlanId = MealPlanId.From(request.MealPlanId);
        var shoppingListId = ShoppingListId.From(request.ShoppingListId);

        var mealPlan = await _mealPlanRepository.GetByIdAsync(mealPlanId, cancellationToken);
        if (mealPlan is null)
        {
            return Error.NotFound(
                "MealPlan.NotFound",
                $"Meal plan '{request.MealPlanId}' was not found.");
        }

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        if (shoppingList is null)
        {
            return Error.NotFound(
                "ShoppingList.NotFound",
                $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        foreach (var entry in mealPlan.Entries.OrderBy(x => x.PlannedDate).ThenBy(x => x.MealType))
        {
            var recipe = await _recipeRepository.GetByIdAsync(entry.RecipeId, cancellationToken);
            if (recipe is null)
            {
                return Error.NotFound(
                    "Recipe.NotFound",
                    $"Recipe '{entry.RecipeId.Value}' was not found.");
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