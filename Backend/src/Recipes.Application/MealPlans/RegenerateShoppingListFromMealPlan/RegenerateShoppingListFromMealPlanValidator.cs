using FluentValidation;

namespace Recipes.Application.MealPlans.RegenerateShoppingListFromMealPlan;

public sealed class RegenerateShoppingListFromMealPlanValidator
    : AbstractValidator<RegenerateShoppingListFromMealPlanCommand>
{
    public RegenerateShoppingListFromMealPlanValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.ShoppingListId).NotEmpty();
    }
}