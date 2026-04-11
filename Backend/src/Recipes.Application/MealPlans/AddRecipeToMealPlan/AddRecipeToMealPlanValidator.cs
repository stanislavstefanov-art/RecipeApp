using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.MealPlans.AddRecipeToMealPlan;

public sealed class AddRecipeToMealPlanValidator : AbstractValidator<AddRecipeToMealPlanCommand>
{
    public AddRecipeToMealPlanValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.MealType)
            .Must(x => Enum.IsDefined(typeof(MealType), x));
    }
}