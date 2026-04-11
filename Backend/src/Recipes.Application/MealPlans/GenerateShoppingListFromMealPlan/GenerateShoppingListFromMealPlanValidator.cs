using FluentValidation;

namespace Recipes.Application.MealPlans.GenerateShoppingListFromMealPlan;

public sealed class GenerateShoppingListFromMealPlanValidator : AbstractValidator<GenerateShoppingListFromMealPlanCommand>
{
    public GenerateShoppingListFromMealPlanValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.ShoppingListId).NotEmpty();
    }
}