using FluentAssertions;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Tests.Entities;

public sealed class MealPlanTests
{
    [Fact]
    public void Constructor_Should_Raise_MealPlanCreated()
    {
        var householdId = HouseholdId.New();
        var mealPlan = new MealPlan("Weekly meals", householdId);

        mealPlan.DomainEvents.Should().ContainSingle(x => x is MealPlanCreated);
    }

    [Fact]
    public void AddRecipe_Should_Add_Entry_With_Assignments()
    {
        var householdId = HouseholdId.New();
        var mealPlan = new MealPlan("Weekly meals", householdId);
        var recipe = new Recipe("Pasta");
        var person1 = new Person("Mom");
        var person2 = new Person("Stanislav");

        mealPlan.AddRecipe(
            recipe,
            new DateOnly(2026, 4, 20),
            MealType.Dinner,
            MealScope.Shared,
            [
                (person1.Id, recipe.Id, null, 1.0m, null),
                (person2.Id, recipe.Id, null, 1.5m, "Higher protein portion")
            ]);

        mealPlan.Entries.Should().HaveCount(1);
        mealPlan.Entries.Single().PersonAssignments.Should().HaveCount(2);
    }

    [Fact]
    public void AddRecipe_Should_Throw_When_Same_Person_Has_Duplicate_Slot()
    {
        var householdId = HouseholdId.New();
        var mealPlan = new MealPlan("Weekly meals", householdId);
        var recipe = new Recipe("Pasta");
        var person = new Person("Mom");
        var date = new DateOnly(2026, 4, 20);

        mealPlan.AddRecipe(
            recipe,
            date,
            MealType.Dinner,
            MealScope.Shared,
            [(person.Id, recipe.Id, null, 1.0m, null)]);

        var action = () => mealPlan.AddRecipe(
            recipe,
            date,
            MealType.Dinner,
            MealScope.Shared,
            [(person.Id, recipe.Id, null, 1.0m, null)]);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdatePersonAssignment_Should_Change_Assigned_Recipe_And_Portion()
    {
        var householdId = HouseholdId.New();
        var mealPlan = new MealPlan("Weekly meals", householdId);

        var recipe1 = new Recipe("Gyuvetche");
        var recipe2 = new Recipe("Fish soup");
        var person = new Person("Sister");

        mealPlan.AddRecipe(
            recipe1,
            new DateOnly(2026, 4, 20),
            MealType.Dinner,
            MealScope.Shared,
            [
                (person.Id, recipe1.Id, null, 1.0m, null)
            ]);

        var entryId = mealPlan.Entries.Single().Id;

        mealPlan.UpdatePersonAssignment(
            entryId,
            person.Id,
            recipe2.Id,
            null,
            1.25m,
            "Swapped to fish soup");

        var assignment = mealPlan.Entries.Single().PersonAssignments.Single();
        assignment.AssignedRecipeId.Should().Be(recipe2.Id);
        assignment.PortionMultiplier.Should().Be(1.25m);
        assignment.Notes.Should().Be("Swapped to fish soup");
    }
}