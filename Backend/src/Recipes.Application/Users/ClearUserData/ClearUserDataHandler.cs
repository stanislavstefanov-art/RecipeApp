using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Users.ClearUserData;

public sealed class ClearUserDataHandler : IRequestHandler<ClearUserDataCommand, ErrorOr<Deleted>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IHouseholdRepository _householdRepository;

    public ClearUserDataHandler(
        ICurrentUser currentUser,
        IExpenseRepository expenseRepository,
        IShoppingListRepository shoppingListRepository,
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository,
        IPersonRepository personRepository,
        IHouseholdRepository householdRepository)
    {
        _currentUser = currentUser;
        _expenseRepository = expenseRepository;
        _shoppingListRepository = shoppingListRepository;
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
        _personRepository = personRepository;
        _householdRepository = householdRepository;
    }

    public async Task<ErrorOr<Deleted>> Handle(ClearUserDataCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);

        // Delete in order: Expenses → ShoppingLists → MealPlans → Recipes → Persons → Households
        var expenses = await _expenseRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);
        _expenseRepository.RemoveRange(expenses);

        var shoppingLists = await _shoppingListRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);
        _shoppingListRepository.RemoveRange(shoppingLists);

        var mealPlans = await _mealPlanRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);
        _mealPlanRepository.RemoveRange(mealPlans);

        var recipes = await _recipeRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);
        _recipeRepository.RemoveRange(recipes);

        var persons = await _personRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);
        _personRepository.RemoveRange(persons);

        var households = await _householdRepository.GetByUserIdAsync(userId, cancellationToken);
        _householdRepository.RemoveRange(households);

        await _householdRepository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
