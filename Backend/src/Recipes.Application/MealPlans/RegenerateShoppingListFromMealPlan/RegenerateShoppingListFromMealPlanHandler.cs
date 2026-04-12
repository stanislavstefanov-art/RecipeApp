using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.RegenerateShoppingListFromMealPlan;

public sealed class RegenerateShoppingListFromMealPlanHandler
    : IRequestHandler<RegenerateShoppingListFromMealPlanCommand, ErrorOr<Success>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly ISender _sender;

    public RegenerateShoppingListFromMealPlanHandler(
        IMealPlanRepository mealPlanRepository,
        IShoppingListRepository shoppingListRepository,
        ISender sender)
    {
        _mealPlanRepository = mealPlanRepository;
        _shoppingListRepository = shoppingListRepository;
        _sender = sender;
    }

    public async Task<ErrorOr<Success>> Handle(
        RegenerateShoppingListFromMealPlanCommand request,
        CancellationToken cancellationToken)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(
            MealPlanId.From(request.MealPlanId),
            cancellationToken);

        if (mealPlan is null)
        {
            return Error.NotFound(
                "MealPlan.NotFound",
                $"Meal plan '{request.MealPlanId}' was not found.");
        }

        var shoppingList = await _shoppingListRepository.GetByIdAsync(
            ShoppingListId.From(request.ShoppingListId),
            cancellationToken);

        if (shoppingList is null)
        {
            return Error.NotFound(
                "ShoppingList.NotFound",
                $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        shoppingList.RemoveGeneratedItems(ShoppingListItemSourceType.MealPlan, mealPlan.Id.Value);
        await _shoppingListRepository.SaveChangesAsync(cancellationToken);

        var generateResult = await _sender.Send(
            new GenerateShoppingListFromMealPlan.GenerateShoppingListFromMealPlanCommand(
                request.MealPlanId,
                request.ShoppingListId),
            cancellationToken);

        if (generateResult.IsError)
        {
            return generateResult.Errors;
        }

        return Result.Success;
    }
}