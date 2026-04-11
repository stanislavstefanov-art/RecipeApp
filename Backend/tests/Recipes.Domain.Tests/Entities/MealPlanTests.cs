using FluentAssertions;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Events;

namespace Recipes.Domain.Tests.Entities;

public sealed class MealPlanTests
{
    [Fact]
    public void Constructor_Should_Raise_MealPlanCreated()
    {
        var mealPlan = new MealPlan("Weekly meals");

        mealPlan.DomainEvents.Should().ContainSingle(x => x is MealPlanCreated);
    }

    [Fact]
    public void AddRecipe_Should_Add_Entry()
    {
        var mealPlan = new MealPlan("Weekly meals");
        var recipe = new Recipe("Pasta");

        mealPlan.AddRecipe(recipe, new DateOnly(2026, 4, 20), MealType.Dinner);

        mealPlan.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void AddRecipe_Should_Throw_For_Duplicate_Slot()
    {
        var mealPlan = new MealPlan("Weekly meals");
        var recipe = new Recipe("Pasta");
        var date = new DateOnly(2026, 4, 20);

        mealPlan.AddRecipe(recipe, date, MealType.Dinner);

        var action = () => mealPlan.AddRecipe(recipe, date, MealType.Dinner);

        action.Should().Throw<InvalidOperationException>();
    }
}