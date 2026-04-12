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

        RuleFor(x => x.Scope)
            .Must(x => Enum.IsDefined(typeof(MealScope), x));

        RuleFor(x => x.Assignments)
            .NotEmpty();

        RuleForEach(x => x.Assignments).ChildRules(a =>
        {
            a.RuleFor(x => x.PersonId).NotEmpty();
            a.RuleFor(x => x.AssignedRecipeId).NotEmpty();
            a.RuleFor(x => x.PortionMultiplier).GreaterThan(0);
            a.RuleFor(x => x.Notes).MaximumLength(1000);
        });
    }
}